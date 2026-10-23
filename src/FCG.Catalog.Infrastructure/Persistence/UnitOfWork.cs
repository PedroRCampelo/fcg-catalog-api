using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FCG.Catalog.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    // Cada índice único do banco é a palavra final sobre uma regra que a aplicação também checa antes.
    // Quando duas requisições passam juntas pela checagem, o banco recusa a segunda e ela vira 409.
    private static readonly Dictionary<string, string> MensagensPorRestricao = new()
    {
        [JogoConfiguration.IndiceDeTituloUnico] = "Já existe um jogo cadastrado com este título.",
        [PedidoConfiguration.IndiceDePedidoPendenteUnico] = "Já existe um pedido pendente para este jogo. Aguarde o resultado do pagamento.",
        [JogoAdquiridoConfiguration.IndiceDeAquisicaoUnica] = "Você já possui este jogo."
    };

    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            var mensagem = pg.ConstraintName is not null && MensagensPorRestricao.TryGetValue(pg.ConstraintName, out var m)
                ? m
                : "O registro conflita com um já existente.";

            throw new ConflitoException(mensagem);
        }
    }
}
