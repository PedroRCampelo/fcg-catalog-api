using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infrastructure.Persistence.Repositories;

public class PromocaoRepository : IPromocaoRepository
{
    private readonly CatalogDbContext _context;

    public PromocaoRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Promocao>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _context.Promocoes.AsNoTracking().OrderByDescending(p => p.DataInicio).ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default) =>
        await _context.Promocoes.AddAsync(promocao, cancellationToken);
}
