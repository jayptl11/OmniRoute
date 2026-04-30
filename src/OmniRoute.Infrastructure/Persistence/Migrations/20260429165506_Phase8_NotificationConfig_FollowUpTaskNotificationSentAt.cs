using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniRoute.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_NotificationConfig_FollowUpTaskNotificationSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationSentAt",
                table: "FollowUpTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NotificationConfigs",
                columns: new[] { "Id", "IsEnabled", "NotificationType", "TargetRole", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("c1000001-0000-0000-0000-000000000001"), true, "NEW_LEAD", "TN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c1000002-0000-0000-0000-000000000002"), true, "SLA_WARNING", "TN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c1000003-0000-0000-0000-000000000003"), true, "SLA_VIOLATED", "TN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c1000004-0000-0000-0000-000000000004"), true, "SLA_VIOLATED", "QL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c1000005-0000-0000-0000-000000000005"), true, "ESCALATED", "TN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c1000006-0000-0000-0000-000000000006"), true, "ESCALATED", "QL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigs_NotificationType_TargetRole",
                table: "NotificationConfigs",
                columns: new[] { "NotificationType", "TargetRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationConfigs");

            migrationBuilder.DropColumn(
                name: "NotificationSentAt",
                table: "FollowUpTasks");
        }
    }
}
