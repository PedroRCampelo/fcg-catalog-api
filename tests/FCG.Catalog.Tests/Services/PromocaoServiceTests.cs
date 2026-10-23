using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Exceptions;
using FCG.Catalog.Tests.Fakes;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public class PromocaoServiceTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeJogoRepository _jogos = new();
    private readonly FakePromocaoRepository _promocoes = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly PromocaoService _servico;

    public PromocaoServiceTests()
    {
        _servico = new PromocaoService(_promocoes, _jogos, _unitOfWork);
    }

    private Jogo AdicionarJogo(bool ativo = true)
    {
        var jogo = new Jogo("Hades", "d", 59.90m);
        if (!ativo) jogo.Desativar();
        _jogos.Jogos.Add(jogo);
        return jogo;
    }

    [Fact]
    public async Task Criar_Deve_Persistir_A_Promocao_Para_O_Jogo()
    {
        var jogo = AdicionarJogo();

        var resposta = await _servico.CriarAsync(new CriarPromocaoRequest(jogo.Id, 25m, Inicio, Fim));

        Assert.Equal(jogo.Id, resposta.JogoId);
        Assert.Equal(25m, resposta.PercentualDesconto);
        Assert.Single(_promocoes.Promocoes);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Criar_Para_Jogo_Inexistente_Deve_Lancar_Nao_Encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _servico.CriarAsync(new CriarPromocaoRequest(Guid.NewGuid(), 25m, Inicio, Fim)));
    }

    [Fact]
    public async Task Criar_Para_Jogo_Desativado_Deve_Lancar_Nao_Encontrado()
    {
        var jogo = AdicionarJogo(ativo: false);

        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _servico.CriarAsync(new CriarPromocaoRequest(jogo.Id, 25m, Inicio, Fim)));
    }

    [Fact]
    public async Task Criar_Com_Periodo_Invalido_Deve_Lancar_Excecao_De_Dominio_Sem_Salvar()
    {
        var jogo = AdicionarJogo();

        await Assert.ThrowsAsync<PromocaoInvalidaException>(
            () => _servico.CriarAsync(new CriarPromocaoRequest(jogo.Id, 25m, Fim, Inicio)));

        Assert.Empty(_promocoes.Promocoes);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Listar_Deve_Retornar_As_Promocoes_Cadastradas()
    {
        var jogo = AdicionarJogo();
        await _servico.CriarAsync(new CriarPromocaoRequest(jogo.Id, 10m, Inicio, Fim));
        await _servico.CriarAsync(new CriarPromocaoRequest(jogo.Id, 30m, Inicio, Fim));

        Assert.Equal(2, (await _servico.ListarAsync()).Count);
    }
}
