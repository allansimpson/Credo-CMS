using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchIndex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsMembersOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndex", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndex_EntityType_EntityId",
                table: "SearchIndex",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndex_IsMembersOnly",
                table: "SearchIndex",
                column: "IsMembersOnly");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndex_IsPublished",
                table: "SearchIndex",
                column: "IsPublished");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchIndex");
        }
    }
}
