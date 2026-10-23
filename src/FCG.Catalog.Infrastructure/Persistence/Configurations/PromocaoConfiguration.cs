using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infrastructure.Persistence.Configurations;

public class PromocaoConfiguration : IEntityTypeConfiguration<Promocao>
{
    public void Configure(EntityTypeBuilder<Promocao> builder)
    {
        builder.ToTable("Promocoes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PercentualDesconto)
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(p => p.DataInicio).IsRequired();
        builder.Property(p => p.DataFim).IsRequired();

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(p => p.JogoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
