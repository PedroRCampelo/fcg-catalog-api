using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Services;

internal static class JogoMapper
{
    public static JogoResponse ParaResponse(Jogo jogo) =>
        new(jogo.Id, jogo.Titulo, jogo.Descricao, jogo.Preco);
}
