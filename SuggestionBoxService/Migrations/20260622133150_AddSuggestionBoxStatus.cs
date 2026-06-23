using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuggestionBoxService.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestionBoxStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SuggestionBoxes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "SuggestionBoxes");
        }
    }
}
