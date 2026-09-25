using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Tickets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedByUserIdToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RequestedByUserId",
                table: "Tickets",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_RequestedByUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "Tickets");
        }
    }
}
