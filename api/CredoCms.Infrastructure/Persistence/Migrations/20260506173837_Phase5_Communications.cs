using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_Communications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminNotificationFrequency",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BlogEmailTargetGroupIdsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BlogEmailTargetMode",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EmailEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailFromAddress",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailFromName",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmailProvider",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmailReplyToAddress",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSubjectPrefixBlog",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailSubjectPrefixNews",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewsEmailTargetGroupIdsJson",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NewsEmailTargetMode",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SendGridApiKey",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SendGridWebhookSecret",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsProvider",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SmtpUseSsl",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestEmailRecipient",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwilioAccountSid",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwilioAuthToken",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwilioFromNumber",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeSigningKey",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledPublishAt",
                table: "News",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendEmailOnPublish",
                table: "News",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReminderEmailSentAt",
                table: "EventRegistrations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendEmailOnPublish",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdminNotificationLastSent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationCategory = table.Column<int>(type: "int", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotificationLastSent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailBroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BroadcastId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmailAddressSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClickedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BouncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BounceReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SendGridMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailBroadcastRecipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailBroadcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetMode = table.Column<int>(type: "int", nullable: false),
                    TargetGroupIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendMode = table.Column<int>(type: "int", nullable: false),
                    ScheduledSendAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecipientCountAtSend = table.Column<int>(type: "int", nullable: true),
                    DeliveredCount = table.Column<int>(type: "int", nullable: false),
                    BouncedCount = table.Column<int>(type: "int", nullable: false),
                    ComplaintCount = table.Column<int>(type: "int", nullable: false),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailBroadcasts", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailBroadcastsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "EmailSuppressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuppressionType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedSource = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSuppressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailableMergeFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailTemplatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "EventVolunteerRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SlotsNeeded = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EventVolunteerRoles", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EventVolunteerRolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "EventVolunteerSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventVolunteerRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedUpAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CanceledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReminderEmailSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventVolunteerSignups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEventLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEventLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotificationLastSent_UserId_NotificationCategory",
                table: "AdminNotificationLastSent",
                columns: new[] { "UserId", "NotificationCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcastRecipients_BroadcastId_Status",
                table: "EmailBroadcastRecipients",
                columns: new[] { "BroadcastId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcastRecipients_SendGridMessageId",
                table: "EmailBroadcastRecipients",
                column: "SendGridMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcastRecipients_UserId",
                table: "EmailBroadcastRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcasts_ScheduledSendAt",
                table: "EmailBroadcasts",
                column: "ScheduledSendAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcasts_SentAt",
                table: "EmailBroadcasts",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailBroadcasts_Status",
                table: "EmailBroadcasts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_CreatedAt",
                table: "EmailSuppressions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_EmailAddress",
                table: "EmailSuppressions",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_SuppressionType",
                table: "EmailSuppressions",
                column: "SuppressionType");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TemplateKey",
                table: "EmailTemplates",
                column: "TemplateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerRoles_EventId",
                table: "EventVolunteerRoles",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerRoles_EventId_DisplayOrder",
                table: "EventVolunteerRoles",
                columns: new[] { "EventId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerSignups_EventId_OccurrenceDate",
                table: "EventVolunteerSignups",
                columns: new[] { "EventId", "OccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerSignups_EventVolunteerRoleId_OccurrenceDate",
                table: "EventVolunteerSignups",
                columns: new[] { "EventVolunteerRoleId", "OccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerSignups_EventVolunteerRoleId_OccurrenceDate_UserId",
                table: "EventVolunteerSignups",
                columns: new[] { "EventVolunteerRoleId", "OccurrenceDate", "UserId" },
                unique: true,
                filter: "[CanceledAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventVolunteerSignups_UserId",
                table: "EventVolunteerSignups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEventLog_EventId",
                table: "WebhookEventLog",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEventLog_ProcessedAt",
                table: "WebhookEventLog",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotificationLastSent");

            migrationBuilder.DropTable(
                name: "EmailBroadcastRecipients");

            migrationBuilder.DropTable(
                name: "EmailBroadcasts")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailBroadcastsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "EmailSuppressions");

            migrationBuilder.DropTable(
                name: "EmailTemplates")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailTemplatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "EventVolunteerRoles")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EventVolunteerRolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "EventVolunteerSignups");

            migrationBuilder.DropTable(
                name: "WebhookEventLog");

            migrationBuilder.DropColumn(
                name: "AdminNotificationFrequency",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BlogEmailTargetGroupIdsJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BlogEmailTargetMode",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailFromAddress",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailFromName",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailProvider",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailReplyToAddress",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailSubjectPrefixBlog",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailSubjectPrefixNews",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NewsEmailTargetGroupIdsJson",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NewsEmailTargetMode",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SendGridApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SendGridWebhookSecret",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmsProvider",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUseSsl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TestEmailRecipient",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TwilioAccountSid",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TwilioAuthToken",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TwilioFromNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "UnsubscribeSigningKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ScheduledPublishAt",
                table: "News");

            migrationBuilder.DropColumn(
                name: "SendEmailOnPublish",
                table: "News");

            migrationBuilder.DropColumn(
                name: "ReminderEmailSentAt",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "SendEmailOnPublish",
                table: "BlogPosts");
        }
    }
}
