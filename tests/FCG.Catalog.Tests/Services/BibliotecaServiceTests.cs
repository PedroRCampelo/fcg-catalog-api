using FCG.Catalog.Application.Services;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Tests.Fakes;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public class BibliotecaServiceTests
{
    private readonly FakeJogoRepository _jogos = new();
    private readonly FakeBibliotecaRepository _biblioteca = new();
    private readonly BibliotecaService _servico;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public BibliotecaServiceTests()
    {
        _servico = new BibliotecaService(_biblioteca, _jogos);
    }

    private Jogo AdicionarJogo(string titulo)
    {
        var jogo = new Jogo(titulo, "d", 10m);
        _jogos.Jogos.Add(jogo);
        return jogo;
    }

    [Fact]
    public async Task Listar_Deve_Retornar_Somente_Os_Jogos_Do_Usuario_Com_Os_Titulos()
    {
        var hades = AdicionarJogo("Hades");
        var celeste = AdicionarJogo("Celeste");
        _biblioteca.Itens.Add(new JogoAdquirido(_usuarioId, hades.Id, Guid.NewGuid()));
        _biblioteca.Itens.Add(new JogoAdquirido(Guid.NewGuid(), celeste.Id, Guid.NewGuid()));

        var resposta = await _servico.ListarAsync(_usuarioId);

        var item = Assert.Single(resposta);
        Assert.Equal("Hades", item.TituloJogo);
        Assert.Equal(hades.Id, item.JogoId);
    }

    [Fact]
    public async Task Jogo_Desativado_Continua_Aparecendo_Na_Biblioteca_De_Quem_Ja_Comprou()
    {
        var hades = AdicionarJogo("Hades");
        _biblioteca.Itens.Add(new JogoAdquirido(_usuarioId, hades.Id, Guid.NewGuid()));

        hades.Desativar();

        var item = Assert.Single(await _servico.ListarAsync(_usuarioId));
        Assert.Equal("Hades", item.TituloJogo);
    }

    [Fact]
    public async Task Listar_Biblioteca_Vazia_Deve_Retornar_Lista_Vazia()
    {
        Assert.Empty(await _servico.ListarAsync(_usuarioId));
    }
}
