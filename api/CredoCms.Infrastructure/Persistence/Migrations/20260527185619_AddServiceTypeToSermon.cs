using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeToSermon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "Sermons",
                type: "int",
                nullable: false,
                defaultValue: 1); // AmWorship

            // Backfill based on PublishedAt time-of-day + day-of-week.
            migrationBuilder.Sql("""
                UPDATE [dbo].[Sermons] SET [ServiceType] = CASE
                    WHEN DATEPART(dw, [PublishedAt]) = 4 THEN 3
                    WHEN DATEPART(hour, [PublishedAt]) < 10 THEN 0
                    WHEN DATEPART(hour, [PublishedAt]) < 13 THEN 1
                    WHEN DATEPART(hour, [PublishedAt]) >= 17 THEN 2
                    ELSE 1
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Sermons");
        }
    }
}
