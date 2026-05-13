using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonymousUserService.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokensAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AnonymousUsers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousUsers_Username",
                table: "AnonymousUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateTable(
                name: "AnonymousRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonymousRefreshTokens_AnonymousUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AnonymousUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousRefreshTokens_Token",
                table: "AnonymousRefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousRefreshTokens_UserId",
                table: "AnonymousRefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AnonymousRefreshTokens");

            migrationBuilder.DropIndex(name: "IX_AnonymousUsers_Username", table: "AnonymousUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AnonymousUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
