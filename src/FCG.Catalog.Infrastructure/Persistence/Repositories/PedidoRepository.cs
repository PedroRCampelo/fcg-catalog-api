using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infrastructure.Persistence.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly CatalogDbContext _context;

    public PedidoRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pedido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        await _context.Pedidos.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistePendenteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default) =>
        await _context.Pedidos.AnyAsync(
            p => p.UsuarioId == usuarioId && p.JogoId == jogoId && p.Status == StatusPedido.Pendente,
            cancellationToken);

    public async Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default) =>
        await _context.Pedidos.AddAsync(pedido, cancellationToken);
}
