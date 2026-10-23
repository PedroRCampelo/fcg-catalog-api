namespace FCG.Catalog.Domain.Entities;

public sealed class JogoAdquirido
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid JogoId { get; private set; }

    // Pedido que originou a aquisição (rastreabilidade entre biblioteca e compra).
    public Guid PedidoId { get; private set; }
    public DateTime DataAquisicao { get; private set; }

    private JogoAdquirido()
    {
    }

    public JogoAdquirido(Guid usuarioId, Guid jogoId, Guid pedidoId)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        JogoId = jogoId;
        PedidoId = pedidoId;
        DataAquisicao = DateTime.UtcNow;
    }
}
