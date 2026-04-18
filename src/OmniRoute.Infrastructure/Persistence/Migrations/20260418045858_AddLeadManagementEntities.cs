using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniRoute.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadManagementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaxHours = table.Column<int>(type: "int", nullable: false),
                    WarningBeforeHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stores_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NeedType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NeedDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductInterest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriorityScore = table.Column<int>(type: "int", nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RoutingType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AssignedGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignedStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SlaViolated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Stores_AssignedStoreId",
                        column: x => x.AssignedStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Leads_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Leads_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TeamType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Teams_Users_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RoutingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false),
                    ConditionChannelJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConditionKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingRules_Teams_ActionTeamId",
                        column: x => x.ActionTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "SlaConfigs",
                columns: new[] { "Id", "AssignedGroup", "IsActive", "MaxHours", "PriorityLevel", "WarningBeforeHours" },
                values: new object[,]
                {
                    { new Guid("a1000001-0000-0000-0000-000000000001"), "Sale", true, 2, "High", 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000002"), "Sale", true, 4, "Medium", 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000003"), "Sale", true, 8, "Low", 2 },
                    { new Guid("a1000001-0000-0000-0000-000000000004"), "Cskh", true, 1, "High", 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000005"), "Cskh", true, 4, "Medium", 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000006"), "Cskh", true, 24, "Low", 4 },
                    { new Guid("a1000001-0000-0000-0000-000000000007"), "StoreSupport", true, 4, "High", 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000008"), "StoreSupport", true, 8, "Medium", 2 },
                    { new Guid("a1000001-0000-0000-0000-000000000009"), "StoreSupport", true, 24, "Low", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EntityType_EntityId",
                table: "ActivityLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_PerformedAt",
                table: "ActivityLogs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_PerformedBy",
                table: "ActivityLogs",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AssignedStoreId",
                table: "Leads",
                column: "AssignedStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AssignedUserId",
                table: "Leads",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CreatedBy",
                table: "Leads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CustomerPhone",
                table: "Leads",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadCode",
                table: "Leads",
                column: "LeadCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_ActionTeamId",
                table: "RoutingRules",
                column: "ActionTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_PriorityOrder",
                table: "RoutingRules",
                column: "PriorityOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigs_AssignedGroup_PriorityLevel",
                table: "SlaConfigs",
                columns: new[] { "AssignedGroup", "PriorityLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_ManagerId",
                table: "Stores",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_StoreCode",
                table: "Stores",
                column: "StoreCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeaderId",
                table: "Teams",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_StoreId",
                table: "Teams",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RoutingRules");

            migrationBuilder.DropTable(
                name: "SlaConfigs");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Stores");
        }
    }
}
