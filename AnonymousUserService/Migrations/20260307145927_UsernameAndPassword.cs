using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonymousUserService.Migrations
{
    /// <inheritdoc />
    public partial class UsernameAndPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxAccessLinkId",
                table: "AnonymousUsers");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "AnonymousUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AnonymousUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "AnonymousUsers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AnonymousUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "BoxAccessLinkId",
                table: "AnonymousUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
