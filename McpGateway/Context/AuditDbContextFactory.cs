using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace McpGateway.Context;

/// <summary>
/// Design-time factory used ONLY by <c>dotnet ef migrations</c> to scaffold migrations without
/// running the app host (which would try to connect / migrate on startup). The connection string is
/// a placeholder — scaffolding reads the model, it does not connect. Runtime uses the DbContextFactory
/// registered in <c>Program.cs</c> instead.
/// </summary>
public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer("Server=localhost;Database=McpAuditDB;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new AuditDbContext(options);
    }
}
