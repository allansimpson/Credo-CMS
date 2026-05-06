using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_AnalyticsAndCookieConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalyticsProvider",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CookiePolicyPageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ga4ConsentBannerEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Ga4ConsentBannerPosition",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ga4MeasurementId",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalyticsProvider",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CookiePolicyPageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Ga4ConsentBannerEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Ga4ConsentBannerPosition",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Ga4MeasurementId",
                table: "SiteSettings");
        }
    }
}
