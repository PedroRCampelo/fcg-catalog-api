using System.Security.Claims;
using FCG.Catalog.Api.Extensions;
using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Services;

namespace FCG.Catalog.Api.Endpoints;

public static class BibliotecaEndpoints
{
    public static IEndpointRouteBuilder MapBibliotecaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/biblioteca")
            .WithTags("Biblioteca")
            .RequireAuthorization();

        grupo.MapPost("/{jogoId:guid}", async (Guid jogoId, ClaimsPrincipal usuario, PedidoService pedidoService, CancellationToken cancellationToken) =>
            {
                var pedido = await pedidoService.ComprarAsync(usuario.ObterUsuarioId(), jogoId, cancellationToken);
                return Results.Accepted("/pedidos", pedido);
            })
            .WithName("ComprarJogo")
            .WithSummary("Inicia a compra de um jogo")
            .WithDescription("Requer usuário autenticado. Não adiciona o jogo à biblioteca: abre um pedido Pendente e responde 202 Accepted. " +
                             "O jogo só entra na biblioteca quando o pagamento for aprovado. Acompanhe o resultado em GET /pedidos.")
            .Produces<PedidoResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("/", async (ClaimsPrincipal usuario, BibliotecaService bibliotecaService, CancellationToken cancellationToken) =>
                Results.Ok(await bibliotecaService.ListarAsync(usuario.ObterUsuarioId(), cancellationToken)))
            .WithName("ListarBiblioteca")
            .WithSummary("Lista os jogos da biblioteca do usuário autenticado")
            .WithDescription("Requer usuário autenticado. Só aparecem jogos cujo pagamento foi aprovado.")
            .Produces<IReadOnlyList<BibliotecaItemResponse>>(StatusCodes.Status200OK);

        return app;
    }
}
