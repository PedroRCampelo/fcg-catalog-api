using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Interfaces;

namespace FCG.Catalog.Application.Services;

public class BibliotecaService
{
    private readonly IBibliotecaRepository _biblioteca;
    private readonly IJogoRepository _jogos;

    public BibliotecaService(IBibliotecaRepository biblioteca, IJogoRepository jogos)
    {
        _biblioteca = biblioteca;
        _jogos = jogos;
    }

    public async Task<IReadOnlyList<BibliotecaItemResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var itens = await _biblioteca.ListarPorUsuarioAsync(usuarioId, cancellationToken);

        // Inclui jogos desativados: sair do catálogo não tira o jogo de quem já o comprou.
        var jogos = (await _jogos.ObterPorIdsAsync(itens.Select(i => i.JogoId), cancellationToken))
            .ToDictionary(j => j.Id);

        return itens
            .OrderByDescending(i => i.DataAquisicao)
            .Select(i => new BibliotecaItemResponse(i.JogoId, jogos[i.JogoId].Titulo, i.DataAquisicao, i.PedidoId))
            .ToList();
    }
}
