using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSermonSeriesContextAndConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Context",
                table: "SermonSeries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeLabel",
                table: "SermonSeries",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedParts",
                table: "SermonSeries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SermonContextsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[\"AM Worship\",\"AM Bible Class\",\"PM Worship\",\"Wednesday Night\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SermonContextsJson", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "PlannedParts", table: "SermonSeries");
            migrationBuilder.DropColumn(name: "ScopeLabel", table: "SermonSeries");
            migrationBuilder.DropColumn(name: "Context", table: "SermonSeries");
        }
    }
}
