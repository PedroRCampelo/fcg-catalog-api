using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Services;

namespace FCG.Catalog.Api.Endpoints;

public static class JogoEndpoints
{
    public static IEndpointRouteBuilder MapJogoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/jogos").WithTags("Jogos");

        grupo.MapGet("/", async (JogoService jogoService, CancellationToken cancellationToken) =>
                Results.Ok(await jogoService.ListarAsync(cancellationToken)))
            .WithName("ListarJogos")
            .WithSummary("Lista o catálogo de jogos ativos")
            .WithDescription("Requer usuário autenticado. Jogos desativados não aparecem.")
            .Produces<IReadOnlyList<JogoResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        grupo.MapGet("/{id:guid}", async (Guid id, JogoService jogoService, CancellationToken cancellationToken) =>
                Results.Ok(await jogoService.ObterAsync(id, cancellationToken)))
            .WithName("ObterJogo")
            .WithSummary("Consulta um jogo ativo")
            .WithDescription("Requer usuário autenticado.")
            .Produces<JogoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        grupo.MapPost("/", async (CriarJogoRequest request, JogoService jogoService, CancellationToken cancellationToken) =>
            {
                var jogo = await jogoService.CriarAsync(request, cancellationToken);
                return Results.Created($"/jogos/{jogo.Id}", jogo);
            })
            .WithName("CriarJogo")
            .WithSummary("Cadastra um novo jogo")
            .WithDescription("Requer role Administrador. O título é único, sem diferenciar maiúsculas.")
            .Produces<JogoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(Policies.Administrador);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarJogoRequest request, JogoService jogoService, CancellationToken cancellationToken) =>
                Results.Ok(await jogoService.AtualizarAsync(id, request, cancellationToken)))
            .WithName("AtualizarJogo")
            .WithSummary("Atualiza título, descrição e preço de um jogo")
            .WithDescription("Requer role Administrador. Pedidos já abertos mantêm o título e o preço da época da compra.")
            .Produces<JogoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(Policies.Administrador);

        grupo.MapDelete("/{id:guid}", async (Guid id, JogoService jogoService, CancellationToken cancellationToken) =>
            {
                await jogoService.DesativarAsync(id, cancellationToken);
                return Results.NoContent();
            })
            .WithName("DesativarJogo")
            .WithSummary("Remove um jogo do catálogo (exclusão lógica)")
            .WithDescription("Requer role Administrador. O registro não é apagado: o jogo apenas deixa de aparecer e de poder ser comprado, e continua na biblioteca de quem já o adquiriu.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Administrador);

        return app;
    }
}
