using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuggestionBoxService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBoxAccessLinkIdFromSuggestionBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxAccessLinkId",
                table: "SuggestionBoxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoxAccessLinkId",
                table: "SuggestionBoxes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
