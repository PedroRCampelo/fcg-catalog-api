namespace FCG.Catalog.Application.Dtos;

public record PedidoResponse(
    Guid Id,
    Guid JogoId,
    string TituloJogo,
    decimal Preco,
    string Status,
    DateTime CriadoEm,
    DateTime? FinalizadoEm);
