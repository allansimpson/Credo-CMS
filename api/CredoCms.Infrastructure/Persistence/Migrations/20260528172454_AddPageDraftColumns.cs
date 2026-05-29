using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPageDraftColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "HasUnpublishedDraft", table: "Pages",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(name: "DraftTitle", table: "Pages",
                type: "nvarchar(max)", nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftBodyJson", table: "Pages",
                type: "nvarchar(max)", nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftExcerpt", table: "Pages",
                type: "nvarchar(500)", maxLength: 500, nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftHeroImageUrl", table: "Pages",
                type: "nvarchar(2000)", maxLength: 2000, nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftHeroImageWebpUrl", table: "Pages",
                type: "nvarchar(2000)", maxLength: 2000, nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftHeroImageAlt", table: "Pages",
                type: "nvarchar(300)", maxLength: 300, nullable: true);

            migrationBuilder.AddColumn<string>(name: "DraftMetaDescription", table: "Pages",
                type: "nvarchar(300)", maxLength: 300, nullable: true);

            migrationBuilder.AddColumn<bool>(name: "DraftIsMembersOnly", table: "Pages",
                type: "bit", nullable: true);

            migrationBuilder.AddColumn<int>(name: "DraftTemplate", table: "Pages",
                type: "int", nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(name: "DraftSavedAt", table: "Pages",
                type: "datetimeoffset", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DraftSavedAt", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftTemplate", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftIsMembersOnly", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftMetaDescription", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftHeroImageAlt", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftHeroImageWebpUrl", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftHeroImageUrl", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftExcerpt", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftBodyJson", table: "Pages");
            migrationBuilder.DropColumn(name: "DraftTitle", table: "Pages");
            migrationBuilder.DropColumn(name: "HasUnpublishedDraft", table: "Pages");
        }
    }
}
