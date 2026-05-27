using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublicSite_AddTemplateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Template",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Template",
                table: "SiteSettings");
        }
    }
}
