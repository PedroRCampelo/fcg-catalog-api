using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Interfaces;

public interface IPromocaoRepository
{
    Task<IReadOnlyList<Promocao>> ListarAsync(CancellationToken cancellationToken = default);
    Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default);
}
