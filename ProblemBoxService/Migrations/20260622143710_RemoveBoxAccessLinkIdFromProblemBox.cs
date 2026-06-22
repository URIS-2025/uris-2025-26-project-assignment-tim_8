using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemBoxService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBoxAccessLinkIdFromProblemBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxAccessLinkId",
                table: "ProblemBoxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoxAccessLinkId",
                table: "ProblemBoxes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
