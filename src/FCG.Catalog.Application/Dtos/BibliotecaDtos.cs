namespace FCG.Catalog.Application.Dtos;

public record BibliotecaItemResponse(Guid JogoId, string TituloJogo, DateTime DataAquisicao, Guid PedidoId);
