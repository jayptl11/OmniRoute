using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniRoute.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUpTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUpTasks_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowUpTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTasks_DueAt",
                table: "FollowUpTasks",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTasks_LeadId",
                table: "FollowUpTasks",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTasks_UserId_IsCompleted",
                table: "FollowUpTasks",
                columns: new[] { "UserId", "IsCompleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowUpTasks");
        }
    }
}
