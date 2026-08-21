using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bla.Infrastructure.Persistence;

// Used only by the dotnet-ef CLI to create migrations; it never connects,
// so the connection string here is a placeholder.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BlaDbContext>
{
    public BlaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlaDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=BlaTasks;User Id=sa;Password=design-time-only;TrustServerCertificate=True")
            .Options;

        return new BlaDbContext(options);
    }
}
