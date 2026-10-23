using FCG.Catalog.Domain.Exceptions;

namespace FCG.Catalog.Domain.Entities;

public sealed class Promocao
{
    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public decimal PercentualDesconto { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }

    private Promocao()
    {
    }

    public Promocao(Guid jogoId, decimal percentualDesconto, DateTime dataInicio, DateTime dataFim)
    {
        if (percentualDesconto <= 0 || percentualDesconto > 100)
            throw new PromocaoInvalidaException("O percentual de desconto deve ser maior que 0 e no máximo 100.");

        // Sempre em UTC: é o único Kind que o PostgreSQL aceita gravar em timestamptz.
        var inicio = ParaUtc(dataInicio);
        var fim = ParaUtc(dataFim);

        if (fim <= inicio)
            throw new PromocaoInvalidaException("A data de fim deve ser posterior à data de início.");

        Id = Guid.NewGuid();
        JogoId = jogoId;
        PercentualDesconto = percentualDesconto;
        DataInicio = inicio;
        DataFim = fim;
    }

    // Data sem fuso (Kind Unspecified) é tratada como UTC. Assumir o fuso da máquina faria a mesma
    // requisição valer horários diferentes no notebook (UTC-3) e no container (UTC).
    private static DateTime ParaUtc(DateTime data) => data.Kind switch
    {
        DateTimeKind.Utc => data,
        DateTimeKind.Local => data.ToUniversalTime(),
        _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
    };
}
