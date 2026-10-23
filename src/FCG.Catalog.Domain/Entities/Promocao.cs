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

    public Promocao(Guid jogoId, decimal percentualDesconto, DateTimeOffset dataInicio, DateTimeOffset dataFim)
    {
        if (percentualDesconto <= 0 || percentualDesconto > 100)
            throw new PromocaoInvalidaException("O percentual de desconto deve ser maior que 0 e no máximo 100.");

        if (dataFim <= dataInicio)
            throw new PromocaoInvalidaException("A data de fim deve ser posterior à data de início.");

        Id = Guid.NewGuid();
        JogoId = jogoId;
        PercentualDesconto = percentualDesconto;

        // Sempre em UTC: é o único Kind que o PostgreSQL aceita gravar em timestamptz.
        DataInicio = dataInicio.UtcDateTime;
        DataFim = dataFim.UtcDateTime;
    }
}
