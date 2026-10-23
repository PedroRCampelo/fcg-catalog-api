using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Domain.Exceptions;
using Xunit;

namespace FCG.Catalog.Tests.Entities;

public class PedidoTests
{
    private static Pedido NovoPedido(Guid? usuarioId = null, Jogo? jogo = null) =>
        Pedido.Criar(usuarioId ?? Guid.NewGuid(), jogo ?? new Jogo("Hades", "d", 59.90m));

    [Fact]
    public void Criar_Deve_Gerar_Pedido_Pendente_Com_Fotografia_Do_Jogo()
    {
        var usuarioId = Guid.NewGuid();
        var jogo = new Jogo("Hades", "d", 59.90m);

        var pedido = Pedido.Criar(usuarioId, jogo);

        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(usuarioId, pedido.UsuarioId);
        Assert.Equal(jogo.Id, pedido.JogoId);
        Assert.Equal("Hades", pedido.TituloJogo);
        Assert.Equal(59.90m, pedido.Preco);
        Assert.Equal(StatusPedido.Pendente, pedido.Status);
        Assert.Null(pedido.FinalizadoEm);
    }

    [Fact]
    public void Pedido_Nao_Deve_Mudar_Quando_O_Jogo_For_Alterado_Depois()
    {
        var jogo = new Jogo("Hades", "d", 59.90m);
        var pedido = Pedido.Criar(Guid.NewGuid(), jogo);

        jogo.Atualizar("Hades II", "d", 99.90m);

        Assert.Equal("Hades", pedido.TituloJogo);
        Assert.Equal(59.90m, pedido.Preco);
    }

    [Fact]
    public void Criar_Com_Jogo_Desativado_Deve_Lancar_Excecao()
    {
        var jogo = new Jogo("Hades", "d", 59.90m);
        jogo.Desativar();

        Assert.Throws<JogoIndisponivelException>(() => Pedido.Criar(Guid.NewGuid(), jogo));
    }

    [Fact]
    public void Concluir_Deve_Marcar_O_Pedido_Como_Concluido_Com_Data()
    {
        var pedido = NovoPedido();

        pedido.Concluir();

        Assert.Equal(StatusPedido.Concluido, pedido.Status);
        Assert.NotNull(pedido.FinalizadoEm);
    }

    [Fact]
    public void Recusar_Deve_Marcar_O_Pedido_Como_Recusado_Com_Data()
    {
        var pedido = NovoPedido();

        pedido.Recusar();

        Assert.Equal(StatusPedido.Recusado, pedido.Status);
        Assert.NotNull(pedido.FinalizadoEm);
    }

    [Fact]
    public void Pedido_Concluido_Nao_Deve_Ser_Concluido_Nem_Recusado_De_Novo()
    {
        var pedido = NovoPedido();
        pedido.Concluir();

        Assert.Throws<TransicaoDePedidoInvalidaException>(() => pedido.Concluir());
        Assert.Throws<TransicaoDePedidoInvalidaException>(() => pedido.Recusar());
        Assert.Equal(StatusPedido.Concluido, pedido.Status);
    }

    [Fact]
    public void Pedido_Recusado_Nao_Deve_Virar_Concluido()
    {
        var pedido = NovoPedido();
        pedido.Recusar();

        Assert.Throws<TransicaoDePedidoInvalidaException>(() => pedido.Concluir());
        Assert.Equal(StatusPedido.Recusado, pedido.Status);
    }
}
