using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CredoCms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRegistrationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventRegistrationFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextMaxLength = table.Column<int>(type: "int", nullable: true),
                    NumberMin = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    NumberMax = table.Column<decimal>(type: "decimal(18,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrationFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmitterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubmitterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubmitterPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FieldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CanceledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmationEmailSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrationFields_EventId_DisplayOrder",
                table: "EventRegistrationFields",
                columns: new[] { "EventId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId",
                table: "EventRegistrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_SubmitterEmail",
                table: "EventRegistrations",
                column: "SubmitterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_UserId",
                table: "EventRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrationFields");

            migrationBuilder.DropTable(
                name: "EventRegistrations");
        }
    }
}
