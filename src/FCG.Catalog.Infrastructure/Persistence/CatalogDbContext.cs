using FCG.Catalog.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<JogoAdquirido> JogosAdquiridos => Set<JogoAdquirido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Tabelas do outbox transacional do MassTransit (eventos pendentes de entrega ao RabbitMQ).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
