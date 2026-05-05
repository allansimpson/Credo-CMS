using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptureReferencesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScriptureReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentEntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Book = table.Column<int>(type: "int", nullable: false),
                    ChapterStart = table.Column<int>(type: "int", nullable: false),
                    VerseStart = table.Column<int>(type: "int", nullable: true),
                    ChapterEnd = table.Column<int>(type: "int", nullable: true),
                    VerseEnd = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptureReferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScriptureReferences_Book_ChapterStart",
                table: "ScriptureReferences",
                columns: new[] { "Book", "ChapterStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ScriptureReferences_ParentEntityType_ParentEntityId",
                table: "ScriptureReferences",
                columns: new[] { "ParentEntityType", "ParentEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScriptureReferences");
        }
    }
}
