using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Tests.Fakes;
using FCG.Contracts;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public class PedidoServiceComprarTests
{
    private readonly List<string> _chamadas = new();
    private readonly FakeJogoRepository _jogos = new();
    private readonly FakePedidoRepository _pedidos = new();
    private readonly FakeBibliotecaRepository _biblioteca = new();
    private readonly FakeEventPublisher _eventos;
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly PedidoService _servico;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public PedidoServiceComprarTests()
    {
        _eventos = new FakeEventPublisher(_chamadas);
        _unitOfWork = new FakeUnitOfWork(_chamadas);
        _servico = new PedidoService(_pedidos, _jogos, _biblioteca, _unitOfWork, _eventos, new ListLogger<PedidoService>());
    }

    private Jogo AdicionarJogo(string titulo = "Hades", decimal preco = 59.90m, bool ativo = true)
    {
        var jogo = new Jogo(titulo, "d", preco);
        if (!ativo) jogo.Desativar();
        _jogos.Jogos.Add(jogo);
        return jogo;
    }

    [Fact]
    public async Task Comprar_Deve_Abrir_Um_Pedido_Pendente_E_Nao_Colocar_O_Jogo_Na_Biblioteca()
    {
        var jogo = AdicionarJogo();

        var resposta = await _servico.ComprarAsync(_usuarioId, jogo.Id);

        Assert.Equal("Pendente", resposta.Status);
        var pedido = Assert.Single(_pedidos.Pedidos);
        Assert.Equal(StatusPedido.Pendente, pedido.Status);
        Assert.Empty(_biblioteca.Itens);
    }

    [Fact]
    public async Task Comprar_Deve_Publicar_OrderPlacedEvent_Com_O_Id_Do_Pedido_Como_OrderId()
    {
        var jogo = AdicionarJogo("Hades", 59.90m);

        var resposta = await _servico.ComprarAsync(_usuarioId, jogo.Id);

        var evento = Assert.IsType<OrderPlacedEvent>(Assert.Single(_eventos.Publicados));
        Assert.Equal(resposta.Id, evento.OrderId);
        Assert.Equal(_usuarioId, evento.UserId);
        Assert.Equal(jogo.Id, evento.GameId);
        Assert.Equal("Hades", evento.Titulo);
        Assert.Equal(59.90m, evento.Price);
    }

    [Fact]
    public async Task Comprar_Deve_Registrar_O_Evento_Antes_De_Salvar_Para_Entrar_Na_Mesma_Transacao()
    {
        var jogo = AdicionarJogo();

        await _servico.ComprarAsync(_usuarioId, jogo.Id);

        Assert.Equal(new[] { "publicar", "salvar" }, _chamadas);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Comprar_Jogo_Inexistente_Deve_Lancar_Nao_Encontrado_Sem_Publicar()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ComprarAsync(_usuarioId, Guid.NewGuid()));

        Assert.Empty(_eventos.Publicados);
        Assert.Empty(_chamadas);
    }

    [Fact]
    public async Task Comprar_Jogo_Desativado_Deve_Lancar_Nao_Encontrado_Sem_Publicar()
    {
        var jogo = AdicionarJogo(ativo: false);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ComprarAsync(_usuarioId, jogo.Id));

        Assert.Empty(_eventos.Publicados);
    }

    [Fact]
    public async Task Comprar_Jogo_Que_O_Usuario_Ja_Possui_Deve_Lancar_Conflito_Sem_Publicar()
    {
        var jogo = AdicionarJogo();
        _biblioteca.Itens.Add(new JogoAdquirido(_usuarioId, jogo.Id, Guid.NewGuid()));

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.ComprarAsync(_usuarioId, jogo.Id));

        Assert.Empty(_pedidos.Pedidos);
        Assert.Empty(_eventos.Publicados);
    }

    [Fact]
    public async Task Comprar_Com_Pedido_Pendente_Para_O_Mesmo_Jogo_Deve_Lancar_Conflito()
    {
        var jogo = AdicionarJogo();
        await _servico.ComprarAsync(_usuarioId, jogo.Id);
        _eventos.Publicados.Clear();

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.ComprarAsync(_usuarioId, jogo.Id));

        Assert.Single(_pedidos.Pedidos);
        Assert.Empty(_eventos.Publicados);
    }

    [Fact]
    public async Task Comprar_Depois_De_Um_Pedido_Recusado_Deve_Ser_Permitido()
    {
        var jogo = AdicionarJogo();
        await _servico.ComprarAsync(_usuarioId, jogo.Id);
        _pedidos.Pedidos.Single().Recusar();

        await _servico.ComprarAsync(_usuarioId, jogo.Id);

        Assert.Equal(2, _pedidos.Pedidos.Count);
        Assert.Equal(1, _pedidos.Pedidos.Count(p => p.Status == StatusPedido.Pendente));
    }

    [Fact]
    public async Task Pedidos_De_Usuarios_Diferentes_Para_O_Mesmo_Jogo_Nao_Se_Bloqueiam()
    {
        var jogo = AdicionarJogo();

        await _servico.ComprarAsync(Guid.NewGuid(), jogo.Id);
        await _servico.ComprarAsync(Guid.NewGuid(), jogo.Id);

        Assert.Equal(2, _pedidos.Pedidos.Count);
    }

    [Fact]
    public async Task Listar_Deve_Retornar_Somente_Os_Pedidos_Do_Usuario()
    {
        var jogo = AdicionarJogo();
        await _servico.ComprarAsync(_usuarioId, jogo.Id);
        await _servico.ComprarAsync(Guid.NewGuid(), jogo.Id);

        var resposta = await _servico.ListarAsync(_usuarioId);

        Assert.Single(resposta);
    }
}
