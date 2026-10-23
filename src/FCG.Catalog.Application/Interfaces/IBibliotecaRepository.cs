using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Interfaces;

public interface IBibliotecaRepository
{
    Task<IReadOnlyList<JogoAdquirido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(JogoAdquirido item, CancellationToken cancellationToken = default);
}
