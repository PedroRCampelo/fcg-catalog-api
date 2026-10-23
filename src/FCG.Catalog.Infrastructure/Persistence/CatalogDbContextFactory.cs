using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCG.Catalog.Infrastructure.Persistence;

// Usada apenas pelo dotnet-ef em tempo de design. Gerar uma migration não abre conexão,
// então a string abaixo é só para o provider saber que o destino é PostgreSQL.
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=fcg_catalog")
            .Options;

        return new CatalogDbContext(options);
    }
}
