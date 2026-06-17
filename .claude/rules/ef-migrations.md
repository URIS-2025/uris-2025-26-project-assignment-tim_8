# EF Core Migrations & Seeding

## Adding a unique index to existing data — dedupe first, in the same migration

`migrationBuilder.CreateIndex(unique: true)` fails at apply time if the column already holds
duplicates. Delete the duplicates **before** the `CreateIndex` call in the same `Up()`. If a
foreign key references the rows you're deleting and uses `DeleteBehavior.Restrict`, repoint
those references to the surviving row first, or the delete itself fails.

| Step (order matters) | Evidence |
|----------------------|----------|
| 1. Repoint FK refs off duplicate rows → kept row | `SubscriptionService/Migrations/20260606124807_UniqueSubscriptionPlanTitle.cs` |
| 2. Delete duplicate rows (keep one per key) | same file |
| 3. `CreateIndex(..., unique: true)` | same file |

```csharp
// keep the lowest Id per Title; repoint subscriptions, then delete dups, THEN index
migrationBuilder.Sql(@"WITH ranked AS (
    SELECT Id, Title, ROW_NUMBER() OVER (PARTITION BY Title ORDER BY Id) rn,
           FIRST_VALUE(Id) OVER (PARTITION BY Title ORDER BY Id) KeepId
    FROM SubscriptionPlans)
    UPDATE s SET s.SubscriptionPlanId = r.KeepId
    FROM Subscriptions s JOIN ranked r ON s.SubscriptionPlanId = r.Id WHERE r.rn > 1;");
// ... DELETE rn > 1 ... then migrationBuilder.CreateIndex(unique: true)
```

Tip: `dotnet ef migrations add` with `--no-build` can emit an **empty** migration if it reuses
a stale assembly — run it without `--no-build` so model changes (e.g. a new `HasIndex`) are picked up.

## Seed canonically and idempotently — only when empty

Seed reference data in a static seeder called from `Program.cs` after `db.Database.Migrate()`,
guarded by the `Testing` environment check. Seed **only when the table is empty** so existing
databases are untouched; copy field values into fresh entities (don't attach shared static
instances to the context).

| Pattern | Evidence |
|---------|----------|
| `if (context.X.Any()) return;` then add canonical rows + `SaveChanges()` | `SubscriptionService/Data/SubscriptionPlanSeeder.cs` |
| Called after Migrate, skipped under Testing | `SubscriptionService/Program.cs` |

EF InMemory does not enforce unique indexes or run raw-SQL migrations — unit-test the seeder
logic; verify the index/dedupe via integration or a real run.
