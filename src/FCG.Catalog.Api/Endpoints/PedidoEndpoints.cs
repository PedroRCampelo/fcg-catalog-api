using System.Security.Claims;
using FCG.Catalog.Api.Extensions;
using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Services;

namespace FCG.Catalog.Api.Endpoints;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos")
            .WithTags("Pedidos")
            .RequireAuthorization();

        grupo.MapGet("/", async (ClaimsPrincipal usuario, PedidoService pedidoService, CancellationToken cancellationToken) =>
                Results.Ok(await pedidoService.ListarAsync(usuario.ObterUsuarioId(), cancellationToken)))
            .WithName("ListarPedidos")
            .WithSummary("Lista os pedidos do usuário autenticado")
            .WithDescription("Requer usuário autenticado. Status: Pendente (aguardando o pagamento), Concluido (jogo na biblioteca) ou Recusado (pagamento rejeitado).")
            .Produces<IReadOnlyList<PedidoResponse>>(StatusCodes.Status200OK);

        return app;
    }
}
