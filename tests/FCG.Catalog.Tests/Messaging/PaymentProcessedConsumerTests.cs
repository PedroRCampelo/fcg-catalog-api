using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Infrastructure.Messaging;
using FCG.Catalog.Tests.Fakes;
using FCG.Contracts;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FCG.Catalog.Tests.Messaging;

// O harness do MassTransit serializa a mensagem de verdade, então estes testes passam pelo contrato
// (record + enum PaymentStatus) e pelo caminho real de consumo, só que com o broker em memória.
public class PaymentProcessedConsumerTests
{
    private sealed class Cenario : IAsyncDisposable
    {
        public ServiceProvider Provider { get; init; } = null!;
        public ITestHarness Harness { get; init; } = null!;
        public FakePedidoRepository Pedidos { get; init; } = null!;
        public FakeBibliotecaRepository Biblioteca { get; init; } = null!;
        public ListLogger<PedidoService> Logger { get; init; } = null!;
        public Pedido Pedido { get; init; } = null!;

        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private static async Task<Cenario> IniciarAsync()
    {
        var pedidos = new FakePedidoRepository();
        var biblioteca = new FakeBibliotecaRepository();
        var jogos = new FakeJogoRepository();
        var logger = new ListLogger<PedidoService>();

        var jogo = new Jogo("Hades", "d", 59.90m);
        jogos.Jogos.Add(jogo);
        var pedido = Pedido.Criar(Guid.NewGuid(), jogo);
        pedidos.Pedidos.Add(pedido);

        var provider = new ServiceCollection()
            .AddSingleton<IPedidoRepository>(pedidos)
            .AddSingleton<IBibliotecaRepository>(biblioteca)
            .AddSingleton<IJogoRepository>(jogos)
            .AddSingleton<IUnitOfWork>(new FakeUnitOfWork())
            .AddSingleton<IEventPublisher>(new FakeEventPublisher())
            .AddSingleton<ILogger<PedidoService>>(logger)
            .AddScoped<PedidoService>()
            .AddMassTransitTestHarness(bus => bus.AddConsumer<PaymentProcessedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        return new Cenario { Provider = provider, Harness = harness, Pedidos = pedidos, Biblioteca = biblioteca, Logger = logger, Pedido = pedido };
    }

    private static PaymentProcessedEvent Evento(Pedido pedido, PaymentStatus status) =>
        new(pedido.Id, pedido.UsuarioId, pedido.JogoId, pedido.TituloJogo, status);

    [Fact]
    public async Task Evento_Aprovado_Recebido_Pelo_Broker_Deve_Concluir_O_Pedido_E_Entregar_O_Jogo()
    {
        await using var c = await IniciarAsync();

        await c.Harness.Bus.Publish(Evento(c.Pedido, PaymentStatus.Approved));

        Assert.True(await c.Harness.Consumed.Any<PaymentProcessedEvent>());
        Assert.Equal(StatusPedido.Concluido, c.Pedido.Status);
        Assert.Single(c.Biblioteca.Itens);
    }

    [Fact]
    public async Task Evento_Rejeitado_Recebido_Pelo_Broker_Deve_Recusar_O_Pedido()
    {
        await using var c = await IniciarAsync();

        await c.Harness.Bus.Publish(Evento(c.Pedido, PaymentStatus.Rejected));

        Assert.True(await c.Harness.Consumed.Any<PaymentProcessedEvent>());
        Assert.Equal(StatusPedido.Recusado, c.Pedido.Status);
        Assert.Empty(c.Biblioteca.Itens);
    }

    [Fact]
    public async Task Mesmo_Evento_Entregue_Duas_Vezes_Pelo_Broker_Nao_Deve_Duplicar_O_Jogo()
    {
        await using var c = await IniciarAsync();
        var evento = Evento(c.Pedido, PaymentStatus.Approved);

        await c.Harness.Bus.Publish(evento);
        await c.Harness.Bus.Publish(evento);

        // Só encerra quando as DUAS mensagens foram consumidas (e a segunda descartada pela regra de negócio).
        Assert.True(await c.Harness.Consumed.Any<PaymentProcessedEvent>(x => c.Logger.Registros.Any(r => r.Mensagem.Contains("descartado sem efeito"))));
        Assert.Single(c.Biblioteca.Itens);
        Assert.Equal(StatusPedido.Concluido, c.Pedido.Status);
    }

    [Fact]
    public async Task Evento_De_Pedido_Desconhecido_Deve_Falhar_Sem_Criar_Nada()
    {
        await using var c = await IniciarAsync();
        var desconhecido = new PaymentProcessedEvent(Guid.NewGuid(), c.Pedido.UsuarioId, c.Pedido.JogoId, "Hades", PaymentStatus.Approved);

        await c.Harness.Bus.Publish(desconhecido);

        Assert.True(await c.Harness.Published.Any<Fault<PaymentProcessedEvent>>());
        Assert.Empty(c.Biblioteca.Itens);
        Assert.Equal(StatusPedido.Pendente, c.Pedido.Status);
    }
}
