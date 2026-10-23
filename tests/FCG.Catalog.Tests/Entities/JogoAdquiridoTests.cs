using FCG.Catalog.Domain.Entities;
using Xunit;

namespace FCG.Catalog.Tests.Entities;

public class JogoAdquiridoTests
{
    [Fact]
    public void Deve_Guardar_Usuario_Jogo_E_O_Pedido_Que_Originou_A_Aquisicao()
    {
        var usuarioId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        var pedidoId = Guid.NewGuid();

        var item = new JogoAdquirido(usuarioId, jogoId, pedidoId);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(usuarioId, item.UsuarioId);
        Assert.Equal(jogoId, item.JogoId);
        Assert.Equal(pedidoId, item.PedidoId);
        Assert.InRange(item.DataAquisicao, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }
}
