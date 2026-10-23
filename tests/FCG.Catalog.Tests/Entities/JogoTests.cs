using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Exceptions;
using Xunit;

namespace FCG.Catalog.Tests.Entities;

public class JogoTests
{
    [Fact]
    public void Deve_Criar_Jogo_Ativo_Quando_Os_Dados_Forem_Validos()
    {
        var jogo = new Jogo("The Witcher 3", "RPG de mundo aberto", 79.90m);

        Assert.NotEqual(Guid.Empty, jogo.Id);
        Assert.Equal("The Witcher 3", jogo.Titulo);
        Assert.Equal(79.90m, jogo.Preco);
        Assert.True(jogo.Ativo);
    }

    [Fact]
    public void Deve_Remover_Espacos_Do_Titulo()
    {
        Assert.Equal("Hades", new Jogo("  Hades  ", null, 10m).Titulo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_Lancar_Excecao_Quando_Titulo_For_Invalido(string? titulo)
    {
        Assert.Throws<TituloJogoInvalidoException>(() => new Jogo(titulo!, "d", 10m));
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Titulo_Passar_Do_Limite_Do_Banco()
    {
        var titulo = new string('a', Jogo.TamanhoMaximoDoTitulo + 1);

        Assert.Throws<TituloJogoInvalidoException>(() => new Jogo(titulo, null, 10m));
        Assert.Equal(Jogo.TamanhoMaximoDoTitulo, new Jogo(new string('a', Jogo.TamanhoMaximoDoTitulo), null, 10m).Titulo.Length);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Descricao_Passar_Do_Limite_Do_Banco()
    {
        var descricao = new string('a', Jogo.TamanhoMaximoDaDescricao + 1);

        Assert.Throws<DescricaoJogoInvalidaException>(() => new Jogo("Jogo", descricao, 10m));
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Preco_For_Negativo()
    {
        Assert.Throws<PrecoInvalidoException>(() => new Jogo("Jogo", null, -0.01m));
    }

    [Fact]
    public void Deve_Aceitar_Preco_Zero_Para_Jogo_Gratuito()
    {
        Assert.Equal(0m, new Jogo("Gratis", null, 0m).Preco);
    }

    [Fact]
    public void Atualizar_Deve_Alterar_Os_Dados_E_Manter_Id_E_Situacao()
    {
        var jogo = new Jogo("Antigo", "d", 10m);
        var id = jogo.Id;

        jogo.Atualizar("Novo", "outra", 20m);

        Assert.Equal(id, jogo.Id);
        Assert.Equal("Novo", jogo.Titulo);
        Assert.Equal("outra", jogo.Descricao);
        Assert.Equal(20m, jogo.Preco);
        Assert.True(jogo.Ativo);
    }

    [Fact]
    public void Atualizar_Com_Dados_Invalidos_Nao_Deve_Alterar_Nada()
    {
        var jogo = new Jogo("Original", "d", 10m);

        Assert.Throws<PrecoInvalidoException>(() => jogo.Atualizar("Outro", "x", -1m));

        Assert.Equal("Original", jogo.Titulo);
        Assert.Equal(10m, jogo.Preco);
    }

    [Fact]
    public void Desativar_Deve_Ser_Exclusao_Logica_Que_Preserva_Os_Dados()
    {
        var jogo = new Jogo("Jogo", "d", 10m);

        jogo.Desativar();

        Assert.False(jogo.Ativo);
        Assert.Equal("Jogo", jogo.Titulo);
        Assert.Equal(10m, jogo.Preco);
    }

    [Fact]
    public void Desativar_Duas_Vezes_Deve_Ser_Inofensivo()
    {
        var jogo = new Jogo("Jogo", null, 10m);

        jogo.Desativar();
        jogo.Desativar();

        Assert.False(jogo.Ativo);
    }
}
