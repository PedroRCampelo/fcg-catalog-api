using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infrastructure.Persistence.Configurations;

public class JogoAdquiridoConfiguration : IEntityTypeConfiguration<JogoAdquirido>
{
    public const string IndiceDeAquisicaoUnica = "UX_JogosAdquiridos_UsuarioId_JogoId";

    public void Configure(EntityTypeBuilder<JogoAdquirido> builder)
    {
        builder.ToTable("JogosAdquiridos");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.DataAquisicao).IsRequired();

        // Garantia adicional, no banco, de que um usuário nunca tem o mesmo jogo duas vezes na
        // biblioteca, mesmo que a checagem da aplicação seja burlada por uma corrida.
        builder.HasIndex(j => new { j.UsuarioId, j.JogoId })
            .IsUnique()
            .HasDatabaseName(IndiceDeAquisicaoUnica);

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(j => j.JogoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(j => j.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
