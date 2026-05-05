using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSermonsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SermonAttachments",
                columns: table => new
                {
                    SermonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SermonAttachments", x => new { x.SermonId, x.DocumentId });
                });

            migrationBuilder.CreateTable(
                name: "Sermons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    YouTubeChannelId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThumbnailBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ThumbnailWebpBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    YouTubePublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Transcript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranscriptSource = table.Column<int>(type: "int", nullable: false),
                    SpeakerLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpeakerNameFreeText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SermonSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsMembersOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sermons", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SermonsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "SermonTags",
                columns: table => new
                {
                    SermonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SermonTags", x => new { x.SermonId, x.TagId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_PublishedAt",
                table: "Sermons",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_SermonSeriesId",
                table: "Sermons",
                column: "SermonSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_Slug",
                table: "Sermons",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_YouTubeVideoId",
                table: "Sermons",
                column: "YouTubeVideoId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SermonTags_TagId",
                table: "SermonTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SermonAttachments");

            migrationBuilder.DropTable(
                name: "Sermons")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SermonsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "SermonTags");
        }
    }
}
