using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuggestionService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAnonymousUserFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestionComments_AnonymousUsers_CommentAuthorId",
                table: "SuggestionComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Suggestions_AnonymousUsers_AnonymousUserId",
                table: "Suggestions");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_AnonymousUsers_VoteAuthorId",
                table: "Votes");

            migrationBuilder.DropTable(
                name: "AnonymousUsers");

            migrationBuilder.DropTable(
                name: "BoxAccessLink");

            migrationBuilder.DropIndex(
                name: "IX_Votes_VoteAuthorId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_AnonymousUserId",
                table: "Suggestions");

            migrationBuilder.DropIndex(
                name: "IX_SuggestionComments_CommentAuthorId",
                table: "SuggestionComments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoxAccessLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxAccessLink", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnonymousUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoxAccessLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonymousUsers_BoxAccessLink_BoxAccessLinkId",
                        column: x => x.BoxAccessLinkId,
                        principalTable: "BoxAccessLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_VoteAuthorId",
                table: "Votes",
                column: "VoteAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_AnonymousUserId",
                table: "Suggestions",
                column: "AnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComments_CommentAuthorId",
                table: "SuggestionComments",
                column: "CommentAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousUsers_BoxAccessLinkId",
                table: "AnonymousUsers",
                column: "BoxAccessLinkId");

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestionComments_AnonymousUsers_CommentAuthorId",
                table: "SuggestionComments",
                column: "CommentAuthorId",
                principalTable: "AnonymousUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suggestions_AnonymousUsers_AnonymousUserId",
                table: "Suggestions",
                column: "AnonymousUserId",
                principalTable: "AnonymousUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_AnonymousUsers_VoteAuthorId",
                table: "Votes",
                column: "VoteAuthorId",
                principalTable: "AnonymousUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
