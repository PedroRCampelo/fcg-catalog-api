using Microsoft.Extensions.Configuration;

namespace FCG.Catalog.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public QueuesSettings Queues { get; set; } = new();

    public static RabbitMqSettings Carregar(IConfiguration configuration)
    {
        var settings = configuration.GetSection(SectionName).Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("Credenciais do RabbitMQ ausentes. Defina RabbitMq__Username e RabbitMq__Password.");

        if (string.IsNullOrWhiteSpace(settings.Queues.PaymentsOrderPlaced))
            throw new InvalidOperationException("Nome da fila do Payments ausente. Defina RabbitMq__Queues__PaymentsOrderPlaced.");

        if (string.IsNullOrWhiteSpace(settings.Queues.CatalogPaymentProcessed))
            throw new InvalidOperationException("Nome da fila do Catalog ausente. Defina RabbitMq__Queues__CatalogPaymentProcessed.");

        return settings;
    }
}

public class QueuesSettings
{
    // Fila do PaymentsAPI para o OrderPlacedEvent. Quem publica o evento (este serviço) a declara no
    // startup, então precisa ler o mesmo nome que o consumidor (no Kubernetes, do mesmo ConfigMap).
    public string PaymentsOrderPlaced { get; set; } = string.Empty;

    // Fila deste serviço para o PaymentProcessedEvent. É o PaymentsAPI quem a declara no startup dele.
    public string CatalogPaymentProcessed { get; set; } = string.Empty;
}
