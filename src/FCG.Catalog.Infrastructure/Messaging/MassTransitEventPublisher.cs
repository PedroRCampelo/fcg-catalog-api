using FCG.Catalog.Application.Interfaces;
using MassTransit;

namespace FCG.Catalog.Infrastructure.Messaging;

// Com o bus outbox ativo, o IPublishEndpoint não fala com o RabbitMQ direto: ele grava a
// mensagem na tabela OutboxMessage junto com o SaveChanges do DbContext.
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken = default)
        where TEvento : class =>
        _publishEndpoint.Publish(evento, cancellationToken);
}
