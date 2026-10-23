using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Exceptions;
using FCG.Catalog.Tests.Fakes;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public class JogoServiceTests
{
    private readonly FakeJogoRepository _jogos = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly JogoService _servico;

    public JogoServiceTests()
    {
        _servico = new JogoService(_jogos, _unitOfWork);
    }

    private Jogo AdicionarJogo(string titulo = "Hades", decimal preco = 59.90m, bool ativo = true)
    {
        var jogo = new Jogo(titulo, "d", preco);
        if (!ativo) jogo.Desativar();
        _jogos.Jogos.Add(jogo);
        return jogo;
    }

    [Fact]
    public async Task Listar_Deve_Retornar_Apenas_Jogos_Ativos()
    {
        AdicionarJogo("Ativo");
        AdicionarJogo("Removido", ativo: false);

        var resposta = await _servico.ListarAsync();

        Assert.Equal("Ativo", Assert.Single(resposta).Titulo);
    }

    [Fact]
    public async Task Criar_Deve_Persistir_O_Jogo_Como_Ativo()
    {
        var resposta = await _servico.CriarAsync(new CriarJogoRequest("Hades", "d", 59.90m));

        Assert.Equal("Hades", resposta.Titulo);
        Assert.Single(_jogos.Jogos);
        Assert.True(_jogos.Jogos[0].Ativo);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Theory]
    [InlineData("Hades")]
    [InlineData("hades")]
    [InlineData("  HADES  ")]
    public async Task Criar_Com_Titulo_Repetido_Deve_Lancar_Conflito_Sem_Diferenciar_Caixa(string titulo)
    {
        AdicionarJogo("Hades");

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarAsync(new CriarJogoRequest(titulo, "d", 10m)));

        Assert.Single(_jogos.Jogos);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Criar_Com_Titulo_De_Jogo_Desativado_Tambem_Deve_Lancar_Conflito()
    {
        AdicionarJogo("Hades", ativo: false);

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarAsync(new CriarJogoRequest("Hades", "d", 10m)));
    }

    [Fact]
    public async Task Criar_Com_Preco_Negativo_Deve_Lancar_Excecao_De_Dominio_Sem_Salvar()
    {
        await Assert.ThrowsAsync<PrecoInvalidoException>(() => _servico.CriarAsync(new CriarJogoRequest("Hades", "d", -1m)));

        Assert.Empty(_jogos.Jogos);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Obter_De_Jogo_Desativado_Deve_Lancar_Nao_Encontrado()
    {
        var removido = AdicionarJogo(ativo: false);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterAsync(removido.Id));
    }

    [Fact]
    public async Task Atualizar_Deve_Alterar_Os_Dados_E_Salvar()
    {
        var jogo = AdicionarJogo("Hades", 59.90m);

        var resposta = await _servico.AtualizarAsync(jogo.Id, new AtualizarJogoRequest("Hades II", "nova", 99.90m));

        Assert.Equal("Hades II", resposta.Titulo);
        Assert.Equal(99.90m, jogo.Preco);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Atualizar_Mantendo_O_Proprio_Titulo_Nao_Deve_Contar_Como_Duplicado()
    {
        var jogo = AdicionarJogo("Hades", 59.90m);

        await _servico.AtualizarAsync(jogo.Id, new AtualizarJogoRequest("hades", "d", 10m));

        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Atualizar_Para_O_Titulo_De_Outro_Jogo_Deve_Lancar_Conflito_Sem_Salvar()
    {
        AdicionarJogo("Hades");
        var outro = AdicionarJogo("Celeste");

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(outro.Id, new AtualizarJogoRequest("Hades", "d", 10m)));

        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Atualizar_Jogo_Desativado_Deve_Lancar_Nao_Encontrado()
    {
        var removido = AdicionarJogo(ativo: false);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.AtualizarAsync(removido.Id, new AtualizarJogoRequest("Novo", "d", 10m)));
    }

    [Fact]
    public async Task Desativar_Deve_Ser_Exclusao_Logica_E_Nao_Remover_O_Registro()
    {
        var jogo = AdicionarJogo();

        await _servico.DesativarAsync(jogo.Id);

        Assert.False(jogo.Ativo);
        Assert.Contains(jogo, _jogos.Jogos);
        Assert.Equal(1, _unitOfWork.Salvamentos);
        Assert.Empty(await _servico.ListarAsync());
    }

    [Fact]
    public async Task Desativar_Jogo_Inexistente_Deve_Lancar_Nao_Encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.DesativarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Desativar_Jogo_Ja_Desativado_Deve_Ser_Idempotente_Sem_Salvar_De_Novo()
    {
        var jogo = AdicionarJogo(ativo: false);

        await _servico.DesativarAsync(jogo.Id);

        Assert.Equal(0, _unitOfWork.Salvamentos);
    }
}
