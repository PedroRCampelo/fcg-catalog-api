using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public const string IndiceDePedidoPendenteUnico = "UX_Pedidos_Pendente_UsuarioId_JogoId";

    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TituloJogo)
            .IsRequired()
            .HasMaxLength(Jogo.TamanhoMaximoDoTitulo);

        builder.Property(p => p.Preco)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CriadoEm).IsRequired();

        // Token de concorrência (coluna de sistema xmin do PostgreSQL, sem coluna extra na tabela).
        // Duas entregas simultâneas do mesmo PaymentProcessedEvent leem o pedido como Pendente; a
        // segunda a gravar falha aqui, é retentada e então descartada por já não estar Pendente.
        builder.Property<uint>("xmin").IsRowVersion();

        // No máximo um pedido pendente por usuário e jogo. Fecha a corrida de dois cliques simultâneos
        // em "comprar", que passariam juntos pela checagem feita na aplicação.
        builder.HasIndex(p => new { p.UsuarioId, p.JogoId })
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(StatusPedido.Pendente)}'")
            .HasDatabaseName(IndiceDePedidoPendenteUnico);

        builder.HasIndex(p => new { p.UsuarioId, p.CriadoEm });

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(p => p.JogoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
