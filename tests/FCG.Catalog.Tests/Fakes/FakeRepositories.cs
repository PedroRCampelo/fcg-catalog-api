using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;

namespace FCG.Catalog.Tests.Fakes;

public class FakeJogoRepository : IJogoRepository
{
    public List<Jogo> Jogos { get; } = new();

    public Task<Jogo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jogos.FirstOrDefault(j => j.Id == id));

    public Task<IReadOnlyList<Jogo>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Jogo>>(Jogos.Where(j => ids.Contains(j.Id)).ToList());

    public Task<IReadOnlyList<Jogo>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Jogo>>(Jogos.Where(j => j.Ativo).OrderBy(j => j.Titulo).ToList());

    public Task<bool> ExisteComTituloAsync(string titulo, Guid? ignorarJogoId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jogos.Any(j => j.Id != ignorarJogoId && string.Equals(j.Titulo, titulo, StringComparison.OrdinalIgnoreCase)));

    public Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default)
    {
        Jogos.Add(jogo);
        return Task.CompletedTask;
    }
}

public class FakePromocaoRepository : IPromocaoRepository
{
    public List<Promocao> Promocoes { get; } = new();

    public Task<IReadOnlyList<Promocao>> ListarAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Promocao>>(Promocoes.ToList());

    public Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default)
    {
        Promocoes.Add(promocao);
        return Task.CompletedTask;
    }
}

public class FakePedidoRepository : IPedidoRepository
{
    public List<Pedido> Pedidos { get; } = new();

    public Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Pedidos.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Pedido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Pedido>>(Pedidos.Where(p => p.UsuarioId == usuarioId).OrderByDescending(p => p.CriadoEm).ToList());

    public Task<bool> ExistePendenteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Pedidos.Any(p => p.UsuarioId == usuarioId && p.JogoId == jogoId && p.Status == StatusPedido.Pendente));

    public Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Pedidos.Add(pedido);
        return Task.CompletedTask;
    }
}

public class FakeBibliotecaRepository : IBibliotecaRepository
{
    public List<JogoAdquirido> Itens { get; } = new();

    public Task<IReadOnlyList<JogoAdquirido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JogoAdquirido>>(Itens.Where(i => i.UsuarioId == usuarioId).ToList());

    public Task<bool> ExisteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(i => i.UsuarioId == usuarioId && i.JogoId == jogoId));

    public Task AdicionarAsync(JogoAdquirido item, CancellationToken cancellationToken = default)
    {
        Itens.Add(item);
        return Task.CompletedTask;
    }
}
