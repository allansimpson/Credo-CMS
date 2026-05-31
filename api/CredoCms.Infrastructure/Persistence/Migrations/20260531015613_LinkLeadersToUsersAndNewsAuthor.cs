using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkLeadersToUsersAndNewsAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthorUserId",
                table: "News",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Leaders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_News_AuthorUserId",
                table: "News",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaders_UserId",
                table: "Leaders",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_AuthorUserId",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Leaders_UserId",
                table: "Leaders");

            migrationBuilder.DropColumn(
                name: "AuthorUserId",
                table: "News");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Leaders");
        }
    }
}
