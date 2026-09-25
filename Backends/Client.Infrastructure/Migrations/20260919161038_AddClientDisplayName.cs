using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Clients.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Clients");
        }
    }
}
