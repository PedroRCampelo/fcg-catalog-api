using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Tests.Fakes;
using FCG.Contracts;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public class PedidoServiceProcessarPagamentoTests
{
    private readonly FakeJogoRepository _jogos = new();
    private readonly FakePedidoRepository _pedidos = new();
    private readonly FakeBibliotecaRepository _biblioteca = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ListLogger<PedidoService> _logger = new();
    private readonly PedidoService _servico;
    private readonly Pedido _pedido;

    public PedidoServiceProcessarPagamentoTests()
    {
        _servico = new PedidoService(_pedidos, _jogos, _biblioteca, _unitOfWork, new FakeEventPublisher(), _logger);

        var jogo = new Jogo("Hades", "d", 59.90m);
        _jogos.Jogos.Add(jogo);
        _pedido = Pedido.Criar(Guid.NewGuid(), jogo);
        _pedidos.Pedidos.Add(_pedido);
    }

    private PaymentProcessedEvent Evento(PaymentStatus status, Guid? orderId = null, Guid? userId = null, Guid? gameId = null) =>
        new(orderId ?? _pedido.Id, userId ?? _pedido.UsuarioId, gameId ?? _pedido.JogoId, _pedido.TituloJogo, status);

    [Fact]
    public async Task Pagamento_Aprovado_Deve_Concluir_O_Pedido_E_Adicionar_O_Jogo_A_Biblioteca()
    {
        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        Assert.Equal(StatusPedido.Concluido, _pedido.Status);
        var item = Assert.Single(_biblioteca.Itens);
        Assert.Equal(_pedido.UsuarioId, item.UsuarioId);
        Assert.Equal(_pedido.JogoId, item.JogoId);
        Assert.Equal(_pedido.Id, item.PedidoId);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Pagamento_Rejeitado_Deve_Recusar_O_Pedido_Sem_Adicionar_Nada_A_Biblioteca()
    {
        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Rejected));

        Assert.Equal(StatusPedido.Recusado, _pedido.Status);
        Assert.Empty(_biblioteca.Itens);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Mesmo_Evento_Aprovado_Duas_Vezes_Nao_Deve_Duplicar_O_Jogo_Na_Biblioteca()
    {
        var evento = Evento(PaymentStatus.Approved);

        await _servico.ProcessarPagamentoAsync(evento);
        await _servico.ProcessarPagamentoAsync(evento);

        Assert.Single(_biblioteca.Itens);
        Assert.Equal(StatusPedido.Concluido, _pedido.Status);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Reentrega_Deve_Ser_Descartada_Com_Log_Que_Explica_O_Motivo()
    {
        var evento = Evento(PaymentStatus.Approved);
        await _servico.ProcessarPagamentoAsync(evento);
        _logger.Registros.Clear();

        await _servico.ProcessarPagamentoAsync(evento);

        var (nivel, mensagem) = Assert.Single(_logger.Registros);
        Assert.Equal(LogLevel.Warning, nivel);
        Assert.Contains("descartado sem efeito", mensagem);
        Assert.Contains(_pedido.Id.ToString(), mensagem);
        Assert.Contains("Concluido", mensagem);
        Assert.Contains("reentrega", mensagem);
    }

    [Fact]
    public async Task Mesmo_Evento_Rejeitado_Duas_Vezes_Deve_Ser_Descartado_Na_Segunda()
    {
        var evento = Evento(PaymentStatus.Rejected);

        await _servico.ProcessarPagamentoAsync(evento);
        await _servico.ProcessarPagamentoAsync(evento);

        Assert.Equal(StatusPedido.Recusado, _pedido.Status);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Evento_Contraditorio_Depois_De_Aprovado_Nao_Deve_Alterar_O_Pedido()
    {
        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Rejected));

        Assert.Equal(StatusPedido.Concluido, _pedido.Status);
        Assert.Single(_biblioteca.Itens);
    }

    [Fact]
    public async Task Aprovacao_Tardia_Depois_De_Recusado_Nao_Deve_Entregar_O_Jogo()
    {
        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Rejected));

        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        Assert.Equal(StatusPedido.Recusado, _pedido.Status);
        Assert.Empty(_biblioteca.Itens);
    }

    [Fact]
    public async Task Pedido_Desconhecido_Deve_Lancar_Excecao_Sem_Salvar()
    {
        var evento = Evento(PaymentStatus.Approved, orderId: Guid.NewGuid());

        await Assert.ThrowsAsync<PedidoDoEventoNaoEncontradoException>(() => _servico.ProcessarPagamentoAsync(evento));

        Assert.Equal(0, _unitOfWork.Salvamentos);
        Assert.Empty(_biblioteca.Itens);
    }

    [Fact]
    public async Task Evento_De_Outro_Usuario_Para_O_Mesmo_Pedido_Deve_Ser_Recusado_Como_Inconsistente()
    {
        var evento = Evento(PaymentStatus.Approved, userId: Guid.NewGuid());

        await Assert.ThrowsAsync<EventoDePagamentoInconsistenteException>(() => _servico.ProcessarPagamentoAsync(evento));

        Assert.Equal(StatusPedido.Pendente, _pedido.Status);
        Assert.Empty(_biblioteca.Itens);
    }

    [Fact]
    public async Task Evento_De_Outro_Jogo_Para_O_Mesmo_Pedido_Deve_Ser_Recusado_Como_Inconsistente()
    {
        var evento = Evento(PaymentStatus.Approved, gameId: Guid.NewGuid());

        await Assert.ThrowsAsync<EventoDePagamentoInconsistenteException>(() => _servico.ProcessarPagamentoAsync(evento));

        Assert.Equal(StatusPedido.Pendente, _pedido.Status);
    }

    [Fact]
    public async Task Status_De_Pagamento_Desconhecido_Nao_Deve_Alterar_O_Pedido()
    {
        var evento = Evento((PaymentStatus)99);

        await Assert.ThrowsAsync<EventoDePagamentoInconsistenteException>(() => _servico.ProcessarPagamentoAsync(evento));

        Assert.Equal(StatusPedido.Pendente, _pedido.Status);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Aprovacao_Quando_O_Jogo_Ja_Esta_Na_Biblioteca_Deve_Concluir_Sem_Duplicar_O_Item()
    {
        _biblioteca.Itens.Add(new JogoAdquirido(_pedido.UsuarioId, _pedido.JogoId, Guid.NewGuid()));

        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        Assert.Equal(StatusPedido.Concluido, _pedido.Status);
        Assert.Single(_biblioteca.Itens);
        Assert.Contains(_logger.Registros, r => r.Nivel == LogLevel.Warning && r.Mensagem.Contains("sem duplicar"));
    }

    [Fact]
    public async Task Aprovacao_De_Um_Pedido_Nao_Deve_Afetar_Pedidos_De_Outros_Usuarios()
    {
        var jogo = _jogos.Jogos.Single();
        var outroPedido = Pedido.Criar(Guid.NewGuid(), jogo);
        _pedidos.Pedidos.Add(outroPedido);

        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        Assert.Equal(StatusPedido.Pendente, outroPedido.Status);
        Assert.Single(_biblioteca.Itens);
    }

    [Fact]
    public async Task Resultado_Nao_Deve_Ser_Logado_Quando_O_Salvamento_Falha()
    {
        _unitOfWork.FalharAoSalvarCom = new ConflitoException("Você já possui este jogo.");

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved)));

        Assert.DoesNotContain(_logger.Registros, r => r.Mensagem.Contains("concluído"));
        Assert.DoesNotContain(_logger.Registros, r => r.Mensagem.Contains("recusado"));
    }

    [Fact]
    public async Task Resultado_Da_Recusa_Tambem_So_Deve_Ser_Logado_Depois_De_Salvar()
    {
        _unitOfWork.FalharAoSalvarCom = new InvalidOperationException("falha ao gravar");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Rejected)));

        Assert.Empty(_logger.Registros);
    }

    [Fact]
    public async Task Aprovacao_Salva_Deve_Logar_Conclusao_Uma_Unica_Vez()
    {
        await _servico.ProcessarPagamentoAsync(Evento(PaymentStatus.Approved));

        Assert.Single(_logger.Registros, r => r.Mensagem.Contains("concluído") && r.Nivel == LogLevel.Information);
    }
}
