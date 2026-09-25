using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Assignments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBacklogRoutingPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every backlog entry that existed before this migration was queued by
            // LevelRoutingMatcher - the RoutingRuleMatcher path silently dropped its failures
            // instead of persisting them until this same change fixed that gap. "" is not a
            // valid RoutingPath and would throw on read, so backfill to Level explicitly.
            migrationBuilder.AddColumn<string>(
                name: "RoutingPath",
                table: "BacklogEntries",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoutingPath",
                table: "BacklogEntries");
        }
    }
}
