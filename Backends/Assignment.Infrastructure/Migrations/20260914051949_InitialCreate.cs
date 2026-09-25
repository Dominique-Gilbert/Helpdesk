using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Assignments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoutingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MinPriority = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Team = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Team = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxConcurrent = table.Column<int>(type: "int", nullable: false),
                    ActiveAssignments = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketReference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutingRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAssignments_RoutingRules_RoutingRuleId",
                        column: x => x.RoutingRuleId,
                        principalTable: "RoutingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketAssignments_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_Rank",
                table: "RoutingRules",
                column: "Rank");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Team",
                table: "Technicians",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAssignments_RoutingRuleId",
                table: "TicketAssignments",
                column: "RoutingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAssignments_TechnicianId",
                table: "TicketAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAssignments_TicketId",
                table: "TicketAssignments",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketAssignments");

            migrationBuilder.DropTable(
                name: "RoutingRules");

            migrationBuilder.DropTable(
                name: "Technicians");
        }
    }
}
