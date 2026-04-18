using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniRoute.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDailyReminderTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyReminderTime",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "DailyReminderTime",
                table: "UserProfiles",
                type: "time",
                nullable: true);
        }
    }
}
