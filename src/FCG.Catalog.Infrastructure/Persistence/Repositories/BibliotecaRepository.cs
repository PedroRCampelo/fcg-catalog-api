using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infrastructure.Persistence.Repositories;

public class BibliotecaRepository : IBibliotecaRepository
{
    private readonly CatalogDbContext _context;

    public BibliotecaRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<JogoAdquirido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        await _context.JogosAdquiridos.AsNoTracking()
            .Where(j => j.UsuarioId == usuarioId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExisteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default) =>
        await _context.JogosAdquiridos.AnyAsync(j => j.UsuarioId == usuarioId && j.JogoId == jogoId, cancellationToken);

    public async Task AdicionarAsync(JogoAdquirido item, CancellationToken cancellationToken = default) =>
        await _context.JogosAdquiridos.AddAsync(item, cancellationToken);
}
