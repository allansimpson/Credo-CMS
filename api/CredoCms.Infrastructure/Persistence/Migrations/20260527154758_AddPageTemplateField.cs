using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPageTemplateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Template",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Set known page slugs to their templates.
            migrationBuilder.Sql("""
                UPDATE [dbo].[Pages] SET [Template] = 1 WHERE [Slug] = 'about' AND [IsDeleted] = 0;
                UPDATE [dbo].[Pages] SET [Template] = 2 WHERE [Slug] = 'im-new' AND [IsDeleted] = 0;
                UPDATE [dbo].[Pages] SET [Template] = 3 WHERE [Slug] = 'what-we-believe' AND [IsDeleted] = 0;
                UPDATE [dbo].[Pages] SET [Template] = 4 WHERE [Slug] = 'contact' AND [IsDeleted] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Template",
                table: "Pages");
        }
    }
}
