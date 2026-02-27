using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuggestionService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoxAccessLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxAccessLink", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnonymousUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BoxAccessLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Suggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuggestionBoxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnonymousUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suggestions_AnonymousUsers_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "AnonymousUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProblemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionCategoriesJoin",
                columns: table => new
                {
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestionCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionCategoriesJoin", x => new { x.SuggestionId, x.SuggestionCategoryId });
                    table.ForeignKey(
                        name: "FK_SuggestionCategoriesJoin_SuggestionCategories_SuggestionCategoryId",
                        column: x => x.SuggestionCategoryId,
                        principalTable: "SuggestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionCategoriesJoin_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuggestionCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommentAuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestionComments_AnonymousUsers_CommentAuthorId",
                        column: x => x.CommentAuthorId,
                        principalTable: "AnonymousUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuggestionComments_SuggestionComments_SuggestionCommentId",
                        column: x => x.SuggestionCommentId,
                        principalTable: "SuggestionComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuggestionComments_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoteAuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_AnonymousUsers_VoteAuthorId",
                        column: x => x.VoteAuthorId,
                        principalTable: "AnonymousUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votes_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousUsers_BoxAccessLinkId",
                table: "AnonymousUsers",
                column: "BoxAccessLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_SuggestionId",
                table: "Attachments",
                column: "SuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionCategoriesJoin_SuggestionCategoryId",
                table: "SuggestionCategoriesJoin",
                column: "SuggestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComments_CommentAuthorId",
                table: "SuggestionComments",
                column: "CommentAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComments_SuggestionCommentId",
                table: "SuggestionComments",
                column: "SuggestionCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComments_SuggestionId",
                table: "SuggestionComments",
                column: "SuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_AnonymousUserId",
                table: "Suggestions",
                column: "AnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_SuggestionId",
                table: "Votes",
                column: "SuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_VoteAuthorId",
                table: "Votes",
                column: "VoteAuthorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "SuggestionCategoriesJoin");

            migrationBuilder.DropTable(
                name: "SuggestionComments");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "SuggestionCategories");

            migrationBuilder.DropTable(
                name: "Suggestions");

            migrationBuilder.DropTable(
                name: "AnonymousUsers");

            migrationBuilder.DropTable(
                name: "BoxAccessLink");
        }
    }
}
