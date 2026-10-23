using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Infrastructure.Messaging;
using FCG.Catalog.Infrastructure.Persistence;
using FCG.Catalog.Infrastructure.Persistence.Repositories;
using FCG.Catalog.Infrastructure.Security;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string ausente. Defina ConnectionStrings__Default.");

        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IJogoRepository, JogoRepository>();
        services.AddScoped<IPromocaoRepository, PromocaoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IBibliotecaRepository, BibliotecaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(JwtValidationSettings.Carregar(configuration));

        var rabbit = RabbitMqSettings.Carregar(configuration);
        services.AddSingleton(rabbit);
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        AdicionarMensageria(services, rabbit);

        return services;
    }

    private static void AdicionarMensageria(IServiceCollection services, RabbitMqSettings rabbit)
    {
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<PaymentProcessedConsumer>();

            // Os eventos ficam na tabela OutboxMessage, na mesma transação do SaveChanges, e um
            // serviço em background entrega ao RabbitMQ. Se o broker estiver fora do ar na hora da
            // compra, o OrderPlacedEvent espera no banco em vez de se perder.
            bus.AddEntityFrameworkOutbox<CatalogDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, (ushort)rabbit.Port, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });

                // Fila com nome fixo e vindo de configuração: o PaymentsAPI, que publica o evento,
                // precisa conhecer o mesmo nome para declará-la antes de este serviço subir.
                cfg.ReceiveEndpoint(rabbit.Queues.CatalogPaymentProcessed, endpoint =>
                {
                    endpoint.ConfigureConsumer<PaymentProcessedConsumer>(context);

                    endpoint.UseMessageRetry(retry =>
                    {
                        // Repetir não conserta um evento sobre pedido inexistente ou incoerente: vai
                        // direto para a fila de erro. Já uma colisão de concorrência (duas entregas
                        // do mesmo evento) é retentada, e na segunda vez o pedido já não está Pendente.
                        retry.Ignore<PedidoDoEventoNaoEncontradoException>();
                        retry.Ignore<EventoDePagamentoInconsistenteException>();
                        retry.Interval(3, TimeSpan.FromSeconds(2));
                    });
                });
            });
        });
    }
}
