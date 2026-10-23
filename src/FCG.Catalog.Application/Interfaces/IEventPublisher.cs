namespace FCG.Catalog.Application.Interfaces;

public interface IEventPublisher
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken = default)
        where TEvento : class;
}
