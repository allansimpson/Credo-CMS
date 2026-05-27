using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSiteSettingsForYouTube : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YouTubeApiKey",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "YouTubeAutoPublishOnSync",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeChannelId",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeDefaultTagsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "YouTubeLastSuccessfulSyncAt",
                table: "SiteSettings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YouTubeLastSyncImportedCount",
                table: "SiteSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeLastSyncStatus",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeOAuthRefreshToken",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "YouTubeSyncEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "YouTubeSyncIntervalMinutes",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YouTubeApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeAutoPublishOnSync",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeChannelId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeDefaultTagsJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeLastSuccessfulSyncAt",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeLastSyncImportedCount",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeLastSyncStatus",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeOAuthRefreshToken",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeSyncEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "YouTubeSyncIntervalMinutes",
                table: "SiteSettings");
        }
    }
}
