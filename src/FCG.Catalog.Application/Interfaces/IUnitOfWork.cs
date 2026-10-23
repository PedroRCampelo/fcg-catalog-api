namespace FCG.Catalog.Application.Interfaces;

public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken cancellationToken = default);
}
