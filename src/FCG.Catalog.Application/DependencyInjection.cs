using FCG.Catalog.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<JogoService>();
        services.AddScoped<PromocaoService>();
        services.AddScoped<PedidoService>();
        services.AddScoped<BibliotecaService>();

        return services;
    }
}
