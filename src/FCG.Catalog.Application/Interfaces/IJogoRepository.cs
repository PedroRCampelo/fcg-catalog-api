using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Interfaces;

public interface IJogoRepository
{
    /// <summary>Busca qualquer jogo, ativo ou não, com rastreamento para alteração.</summary>
    Task<Jogo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Busca vários jogos, inclusive desativados (uma biblioteca antiga ainda aponta para eles).</summary>
    Task<IReadOnlyList<Jogo>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Jogo>> ListarAtivosAsync(CancellationToken cancellationToken = default);

    /// <summary>Compara sem diferenciar maiúsculas e considera também os jogos desativados.</summary>
    Task<bool> ExisteComTituloAsync(string titulo, Guid? ignorarJogoId = null, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default);
}
