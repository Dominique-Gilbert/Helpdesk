using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Clients.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clients",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Clients",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Clients");
        }
    }
}
