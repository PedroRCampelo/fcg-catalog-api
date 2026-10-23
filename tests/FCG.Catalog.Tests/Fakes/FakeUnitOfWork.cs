using FCG.Catalog.Application.Interfaces;

namespace FCG.Catalog.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<string> _chamadas;

    public FakeUnitOfWork(List<string>? chamadas = null)
    {
        _chamadas = chamadas ?? new List<string>();
    }

    public int Salvamentos { get; private set; }

    public Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        Salvamentos++;
        _chamadas.Add("salvar");
        return Task.CompletedTask;
    }
}
