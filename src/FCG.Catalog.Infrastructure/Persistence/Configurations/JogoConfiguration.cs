using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infrastructure.Persistence.Configurations;

public class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public const string IndiceDeTituloUnico = "IX_Jogos_Titulo";

    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.ToTable("Jogos");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Titulo)
            .IsRequired()
            .HasMaxLength(Jogo.TamanhoMaximoDoTitulo);

        builder.HasIndex(j => j.Titulo)
            .IsUnique()
            .HasDatabaseName(IndiceDeTituloUnico);

        builder.Property(j => j.Descricao)
            .HasMaxLength(Jogo.TamanhoMaximoDaDescricao);

        builder.Property(j => j.Preco)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(j => j.Ativo)
            .IsRequired();
    }
}
