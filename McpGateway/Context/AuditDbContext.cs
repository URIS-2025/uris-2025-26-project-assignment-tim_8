using McpGateway.Audit;
using Microsoft.EntityFrameworkCore;

namespace McpGateway.Context;

/// <summary>
/// EF Core context for the durable audit store (McpAuditDB). One table: <see cref="AuditEntry"/>.
/// Enums persist as their int codes (EF default). Indexes cover the columns the audit dashboard
/// filters/sorts by so the query stays cheap as the table grows.
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<AuditEntry>();
        e.HasKey(x => x.Id);

        // Bounded-offset browsing is Timestamp-desc; filters hit these columns.
        e.HasIndex(x => x.Timestamp);
        e.HasIndex(x => x.OrganizationId);
        e.HasIndex(x => x.AgentId);
        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.ToolName);
        e.HasIndex(x => x.Decision);
        e.HasIndex(x => x.Outcome);
    }
}
