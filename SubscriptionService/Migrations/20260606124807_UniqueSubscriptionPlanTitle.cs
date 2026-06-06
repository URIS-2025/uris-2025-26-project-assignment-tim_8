using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionService.Migrations
{
    /// <inheritdoc />
    public partial class UniqueSubscriptionPlanTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // De-duplicate existing plans before creating the unique index, otherwise
            // index creation fails on databases that already contain duplicate titles
            // (e.g. "basic" seeded twice). Keep the lowest Id per title.

            // 1) Repoint any subscriptions that reference a duplicate plan to the kept plan
            //    (the FK uses DeleteBehavior.Restrict, so orphaning the delete would fail).
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT Id, Title,
                           ROW_NUMBER() OVER (PARTITION BY Title ORDER BY Id) AS rn,
                           FIRST_VALUE(Id) OVER (PARTITION BY Title ORDER BY Id) AS KeepId
                    FROM SubscriptionPlans
                )
                UPDATE s
                SET s.SubscriptionPlanId = r.KeepId
                FROM Subscriptions s
                INNER JOIN ranked r ON s.SubscriptionPlanId = r.Id
                WHERE r.rn > 1;");

            // 2) Delete the duplicate plan rows (all but the kept one per title).
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY Title ORDER BY Id) AS rn
                    FROM SubscriptionPlans
                )
                DELETE FROM SubscriptionPlans
                WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1);");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Title",
                table: "SubscriptionPlans",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Title",
                table: "SubscriptionPlans");
        }
    }
}
