using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpGateway.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToolName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsWrite = table.Column<bool>(type: "bit", nullable: false),
                    ArgsSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confirmation = table.Column<int>(type: "int", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_AgentId",
                table: "AuditEntries",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Decision",
                table: "AuditEntries",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OrganizationId",
                table: "AuditEntries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Outcome",
                table: "AuditEntries",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Timestamp",
                table: "AuditEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ToolName",
                table: "AuditEntries",
                column: "ToolName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_UserId",
                table: "AuditEntries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}
