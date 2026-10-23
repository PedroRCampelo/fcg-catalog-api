using FCG.Catalog.Application.Services;
using FCG.Contracts;
using MassTransit;

namespace FCG.Catalog.Infrastructure.Messaging;

// Só traduz a mensagem em chamada: toda a regra (inclusive a idempotência) está no PedidoService.
public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly PedidoService _pedidos;

    public PaymentProcessedConsumer(PedidoService pedidos)
    {
        _pedidos = pedidos;
    }

    public Task Consume(ConsumeContext<PaymentProcessedEvent> context) =>
        _pedidos.ProcessarPagamentoAsync(context.Message, context.CancellationToken);
}
