using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Application.Services;

internal static class PedidoMapper
{
    public static PedidoResponse ParaResponse(Pedido pedido) =>
        new(pedido.Id, pedido.JogoId, pedido.TituloJogo, pedido.Preco, pedido.Status.ToString(), pedido.CriadoEm, pedido.FinalizadoEm);
}
