using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Interfaces;

public interface IPedidoRepository
{
    /// <summary>Com rastreamento: o pedido buscado aqui é alterado e salvo pelo caso de uso.</summary>
    Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pedido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<bool> ExistePendenteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default);
}
