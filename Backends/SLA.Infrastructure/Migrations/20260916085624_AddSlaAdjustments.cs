using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Sla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeltaMinutes = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    AdjustedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AdjustedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adjustments_SlaClocks_SlaClockId",
                        column: x => x.SlaClockId,
                        principalTable: "SlaClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_SlaClockId",
                table: "Adjustments",
                column: "SlaClockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjustments");
        }
    }
}
