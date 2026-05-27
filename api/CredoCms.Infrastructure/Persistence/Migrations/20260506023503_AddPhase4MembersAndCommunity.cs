using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4MembersAndCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsListedInDirectory",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhotoAltText",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoBlobUrl",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoWebpBlobUrl",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicAuthorBio",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveBlogEmails",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveBroadcastEmails",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveGroupEmailsGlobal",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveNewsEmails",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowAddressInDirectory",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowEmailInDirectory",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPhoneInDirectory",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPhotoInDirectory",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StateOrRegion",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlogCategoriesJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlogPageLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClassAudienceAgeGroupsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClassesPageLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CloudflareTurnstileSecretKey",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudflareTurnstileSiteKey",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectCardAcknowledgmentMessageJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectCardInterestsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConnectCardPageLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FacebookLoginEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FacebookOAuthAppId",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookOAuthAppSecret",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GetInvolvedPageLabel",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PrayerRequestArchiveDays",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PrayerRequestRequireApproval",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfanityAllowlist",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfanityWordlist",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecentPastClassesLookbackDays",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowRecentPastOnPublicClasses",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HeroImageBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HeroImageWebpBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HeroImageAltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelatedSermonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsMembersOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledPublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReadingTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "BlogPostsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "BlogPostTags",
                columns: table => new
                {
                    BlogPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPostTags", x => new { x.BlogPostId, x.TagId });
                });

            migrationBuilder.CreateTable(
                name: "ClassOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TeacherLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeacherFreeText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DetailedScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialsNeeded = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOfferings", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClassOfferingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "ClassSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AudienceAgeGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GeneralMeetingTime = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultRoom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageWebpBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageAltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSlots", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClassSlotsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "ConnectCardSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFirstTimeVisitor = table.Column<bool>(type: "bit", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HowDidYouHear = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    InterestCheckboxesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgmentEmailSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddressHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectCardSubmissions", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ConnectCardSubmissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "GroupMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLeader = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinRequestMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiveGroupEmails = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageWebpBlobUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageAltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MeetingInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Joinability = table.Column<int>(type: "int", nullable: false),
                    RequiresMessageOnJoinRequest = table.Column<int>(type: "int", nullable: false),
                    RosterVisibility = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "GroupsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "PrayerRequestPrayedFor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrayerRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrayedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrayerRequestPrayedFor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrayerRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrayerRequests", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PrayerRequestsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "PrayerRequestUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrayerRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostedByLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrayerRequestUpdates", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PrayerRequestUpdatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsListedInDirectory",
                table: "Users",
                column: "IsListedInDirectory");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_AuthorUserId",
                table: "BlogPosts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Category",
                table: "BlogPosts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsPublished",
                table: "BlogPosts",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_PublishedAt",
                table: "BlogPosts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostTags_TagId",
                table: "BlogPostTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassOfferings_ClassSlotId",
                table: "ClassOfferings",
                column: "ClassSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassOfferings_StartDate_EndDate",
                table: "ClassOfferings",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSlots_AudienceAgeGroup",
                table: "ClassSlots",
                column: "AudienceAgeGroup");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSlots_IsActive",
                table: "ClassSlots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSlots_Slug",
                table: "ClassSlots",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectCardSubmissions_IpAddressHash",
                table: "ConnectCardSubmissions",
                column: "IpAddressHash");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectCardSubmissions_Status",
                table: "ConnectCardSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectCardSubmissions_SubmittedAt",
                table: "ConnectCardSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_GroupId_IsLeader",
                table: "GroupMemberships",
                columns: new[] { "GroupId", "IsLeader" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_GroupId_UserId_Status",
                table: "GroupMemberships",
                columns: new[] { "GroupId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_UserId",
                table: "GroupMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsActive",
                table: "Groups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Slug",
                table: "Groups",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Visibility",
                table: "Groups",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequestPrayedFor_PrayerRequestId_UserId",
                table: "PrayerRequestPrayedFor",
                columns: new[] { "PrayerRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequestPrayedFor_UserId",
                table: "PrayerRequestPrayedFor",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequests_CreatedAt",
                table: "PrayerRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequests_Status",
                table: "PrayerRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequests_SubmittedByUserId",
                table: "PrayerRequests",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrayerRequestUpdates_PrayerRequestId",
                table: "PrayerRequestUpdates",
                column: "PrayerRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "BlogPostsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "BlogPostTags");

            migrationBuilder.DropTable(
                name: "ClassOfferings")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClassOfferingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "ClassSlots")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClassSlotsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "ConnectCardSubmissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ConnectCardSubmissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "GroupMemberships");

            migrationBuilder.DropTable(
                name: "Groups")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "GroupsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "PrayerRequestPrayedFor");

            migrationBuilder.DropTable(
                name: "PrayerRequests")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PrayerRequestsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "PrayerRequestUpdates")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PrayerRequestUpdatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsListedInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsListedInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoAltText",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoBlobUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoWebpBlobUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PublicAuthorBio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReceiveBlogEmails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReceiveBroadcastEmails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReceiveGroupEmailsGlobal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReceiveNewsEmails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowAddressInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowEmailInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowPhoneInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowPhotoInDirectory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StateOrRegion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BlogCategoriesJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BlogPageLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ClassAudienceAgeGroupsJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ClassesPageLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CloudflareTurnstileSecretKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CloudflareTurnstileSiteKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ConnectCardAcknowledgmentMessageJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ConnectCardInterestsJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ConnectCardPageLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FacebookLoginEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FacebookOAuthAppId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FacebookOAuthAppSecret",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GetInvolvedPageLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrayerRequestArchiveDays",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrayerRequestRequireApproval",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ProfanityAllowlist",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ProfanityWordlist",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "RecentPastClassesLookbackDays",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowRecentPastOnPublicClasses",
                table: "SiteSettings");
        }
    }
}
