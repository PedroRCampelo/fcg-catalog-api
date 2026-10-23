using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infrastructure.Persistence.Repositories;

public class JogoRepository : IJogoRepository
{
    private readonly CatalogDbContext _context;

    public JogoRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Jogo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Jogos.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Jogo>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var lista = ids.Distinct().ToList();
        return await _context.Jogos.AsNoTracking().Where(j => lista.Contains(j.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Jogo>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
        await _context.Jogos.AsNoTracking().Where(j => j.Ativo).OrderBy(j => j.Titulo).ToListAsync(cancellationToken);

    public async Task<bool> ExisteComTituloAsync(string titulo, Guid? ignorarJogoId = null, CancellationToken cancellationToken = default)
    {
        var normalizado = titulo.Trim().ToLower();

        return await _context.Jogos.AnyAsync(
            j => j.Id != ignorarJogoId && j.Titulo.ToLower() == normalizado,
            cancellationToken);
    }

    public async Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default) =>
        await _context.Jogos.AddAsync(jogo, cancellationToken);
}
