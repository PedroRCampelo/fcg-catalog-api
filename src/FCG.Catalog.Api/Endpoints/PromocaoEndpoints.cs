using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Services;

namespace FCG.Catalog.Api.Endpoints;

public static class PromocaoEndpoints
{
    public static IEndpointRouteBuilder MapPromocaoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/promocoes").WithTags("Promocoes");

        grupo.MapGet("/", async (PromocaoService promocaoService, CancellationToken cancellationToken) =>
                Results.Ok(await promocaoService.ListarAsync(cancellationToken)))
            .WithName("ListarPromocoes")
            .WithSummary("Lista as promoções cadastradas")
            .WithDescription("Requer usuário autenticado.")
            .Produces<IReadOnlyList<PromocaoResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        grupo.MapPost("/", async (CriarPromocaoRequest request, PromocaoService promocaoService, CancellationToken cancellationToken) =>
            {
                var promocao = await promocaoService.CriarAsync(request, cancellationToken);
                return Results.Created($"/promocoes/{promocao.Id}", promocao);
            })
            .WithName("CriarPromocao")
            .WithSummary("Cadastra uma promoção para um jogo")
            .WithDescription("Requer role Administrador. As datas são interpretadas e guardadas em UTC.")
            .Produces<PromocaoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Administrador);

        return app;
    }
}
