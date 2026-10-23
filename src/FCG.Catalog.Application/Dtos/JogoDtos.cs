namespace FCG.Catalog.Application.Dtos;

public record CriarJogoRequest(string Titulo, string? Descricao, decimal Preco);

public record AtualizarJogoRequest(string Titulo, string? Descricao, decimal Preco);

public record JogoResponse(Guid Id, string Titulo, string? Descricao, decimal Preco);
