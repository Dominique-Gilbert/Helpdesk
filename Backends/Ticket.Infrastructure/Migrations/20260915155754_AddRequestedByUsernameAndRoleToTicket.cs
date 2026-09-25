using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Tickets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedByUsernameAndRoleToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedByRole",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUsername",
                table: "Tickets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedByRole",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RequestedByUsername",
                table: "Tickets");
        }
    }
}
