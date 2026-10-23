using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Services;

public class PromocaoService
{
    private readonly IPromocaoRepository _promocoes;
    private readonly IJogoRepository _jogos;
    private readonly IUnitOfWork _unitOfWork;

    public PromocaoService(IPromocaoRepository promocoes, IJogoRepository jogos, IUnitOfWork unitOfWork)
    {
        _promocoes = promocoes;
        _jogos = jogos;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PromocaoResponse>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var promocoes = await _promocoes.ListarAsync(cancellationToken);
        return promocoes.Select(ParaResponse).ToList();
    }

    public async Task<PromocaoResponse> CriarAsync(CriarPromocaoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogos.ObterPorIdAsync(request.JogoId, cancellationToken);

        if (jogo is not { Ativo: true })
            throw new NaoEncontradoException("Jogo não encontrado.");

        var promocao = new Promocao(jogo.Id, request.PercentualDesconto, request.DataInicio, request.DataFim);

        await _promocoes.AdicionarAsync(promocao, cancellationToken);
        await _unitOfWork.SalvarAsync(cancellationToken);

        return ParaResponse(promocao);
    }

    private static PromocaoResponse ParaResponse(Promocao promocao) =>
        new(promocao.Id, promocao.JogoId, promocao.PercentualDesconto, promocao.DataInicio, promocao.DataFim);
}
