using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Services;

public class JogoService
{
    private readonly IJogoRepository _jogos;
    private readonly IUnitOfWork _unitOfWork;

    public JogoService(IJogoRepository jogos, IUnitOfWork unitOfWork)
    {
        _jogos = jogos;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<JogoResponse>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var jogos = await _jogos.ListarAtivosAsync(cancellationToken);
        return jogos.Select(JogoMapper.ParaResponse).ToList();
    }

    public async Task<JogoResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default) =>
        JogoMapper.ParaResponse(await ObterAtivoOuFalharAsync(id, cancellationToken));

    public async Task<JogoResponse> CriarAsync(CriarJogoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = new Jogo(request.Titulo, request.Descricao, request.Preco);

        await GarantirTituloLivreAsync(jogo.Titulo, null, cancellationToken);

        await _jogos.AdicionarAsync(jogo, cancellationToken);
        await _unitOfWork.SalvarAsync(cancellationToken);

        return JogoMapper.ParaResponse(jogo);
    }

    public async Task<JogoResponse> AtualizarAsync(Guid id, AtualizarJogoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterAtivoOuFalharAsync(id, cancellationToken);

        jogo.Atualizar(request.Titulo, request.Descricao, request.Preco);

        await GarantirTituloLivreAsync(jogo.Titulo, jogo.Id, cancellationToken);
        await _unitOfWork.SalvarAsync(cancellationToken);

        return JogoMapper.ParaResponse(jogo);
    }

    // Exclusão lógica: o registro permanece, porque pedidos e bibliotecas apontam para ele.
    // Repetir a chamada não é erro (DELETE é idempotente).
    public async Task DesativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogos.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException("Jogo não encontrado.");

        if (!jogo.Ativo)
            return;

        jogo.Desativar();
        await _unitOfWork.SalvarAsync(cancellationToken);
    }

    private async Task<Jogo> ObterAtivoOuFalharAsync(Guid id, CancellationToken cancellationToken)
    {
        var jogo = await _jogos.ObterPorIdAsync(id, cancellationToken);

        return jogo is { Ativo: true } ? jogo : throw new NaoEncontradoException("Jogo não encontrado.");
    }

    private async Task GarantirTituloLivreAsync(string titulo, Guid? ignorarJogoId, CancellationToken cancellationToken)
    {
        if (await _jogos.ExisteComTituloAsync(titulo, ignorarJogoId, cancellationToken))
            throw new ConflitoException("Já existe um jogo cadastrado com este título.");
    }
}
