using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Assignments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianLevelsAndBacklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BacklogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketReference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartingLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacklogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianCategoryLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianCategoryLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianCategoryLevels_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BacklogEntries_QueuedAtUtc",
                table: "BacklogEntries",
                column: "QueuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BacklogEntries_TicketId",
                table: "BacklogEntries",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianCategoryLevels_TechnicianId_Category",
                table: "TechnicianCategoryLevels",
                columns: new[] { "TechnicianId", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacklogEntries");

            migrationBuilder.DropTable(
                name: "TechnicianCategoryLevels");
        }
    }
}
