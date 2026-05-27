using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2SiteSettingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defaults below intentionally match the property-level defaults on
            // SiteSettings.cs so existing Phase 1 rows are populated with the
            // same values a freshly-seeded Phase 2 row would have.

            migrationBuilder.AddColumn<string>(
                name: "DefaultMetaDescription",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentCategoriesJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[\"Bulletins\",\"Forms\",\"Policies\",\"Board Minutes\",\"Resources\"]");

            migrationBuilder.AddColumn<string>(
                name: "HomepageHeroCtaLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Join us Sunday");

            migrationBuilder.AddColumn<string>(
                name: "HomepageHeroCtaLink",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "#service-times");

            migrationBuilder.AddColumn<int>(
                name: "ImageMaxWidth",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 2400);

            migrationBuilder.AddColumn<int>(
                name: "ImageQuality",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 82);

            migrationBuilder.AddColumn<string>(
                name: "LeaderCategoriesJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[\"Pastoral Staff\",\"Elders\",\"Deacons\",\"Ministry Directors\"]");

            migrationBuilder.AddColumn<string>(
                name: "LeadersPageLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Our Leaders");

            migrationBuilder.AddColumn<long>(
                name: "MaxDocumentSizeBytes",
                table: "SiteSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 26214400L);

            migrationBuilder.AddColumn<long>(
                name: "MaxImageSizeBytes",
                table: "SiteSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 10485760L);

            migrationBuilder.AddColumn<string>(
                name: "MembersWelcomeText",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMetaDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DocumentCategoriesJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HomepageHeroCtaLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HomepageHeroCtaLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ImageMaxWidth",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ImageQuality",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LeaderCategoriesJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LeadersPageLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MaxDocumentSizeBytes",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MaxImageSizeBytes",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MembersWelcomeText",
                table: "SiteSettings");
        }
    }
}
