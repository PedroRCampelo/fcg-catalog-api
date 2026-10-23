namespace FCG.Catalog.Application.Dtos;

public record CriarPromocaoRequest(Guid JogoId, decimal PercentualDesconto, DateTime DataInicio, DateTime DataFim);

public record PromocaoResponse(Guid Id, Guid JogoId, decimal PercentualDesconto, DateTime DataInicio, DateTime DataFim);
