using FCG.Catalog.Domain.Exceptions;

namespace FCG.Catalog.Domain.Entities;

public sealed class Jogo
{
    public const int TamanhoMaximoDoTitulo = 200;
    public const int TamanhoMaximoDaDescricao = 2000;

    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }

    // A exclusão é sempre lógica: pedidos e bibliotecas continuam apontando para o jogo.
    public bool Ativo { get; private set; }

    private Jogo()
    {
    }

    public Jogo(string titulo, string? descricao, decimal preco)
    {
        Validar(titulo, descricao, preco);

        Id = Guid.NewGuid();
        Titulo = titulo.Trim();
        Descricao = descricao;
        Preco = preco;
        Ativo = true;
    }

    public void Atualizar(string titulo, string? descricao, decimal preco)
    {
        Validar(titulo, descricao, preco);

        Titulo = titulo.Trim();
        Descricao = descricao;
        Preco = preco;
    }

    public void Desativar() => Ativo = false;

    private static void Validar(string titulo, string? descricao, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new TituloJogoInvalidoException("O título do jogo é obrigatório.");

        if (titulo.Trim().Length > TamanhoMaximoDoTitulo)
            throw new TituloJogoInvalidoException($"O título do jogo deve ter no máximo {TamanhoMaximoDoTitulo} caracteres.");

        if (descricao is not null && descricao.Length > TamanhoMaximoDaDescricao)
            throw new DescricaoJogoInvalidaException($"A descrição do jogo deve ter no máximo {TamanhoMaximoDaDescricao} caracteres.");

        if (preco < 0)
            throw new PrecoInvalidoException("O preço do jogo não pode ser negativo.");
    }
}
