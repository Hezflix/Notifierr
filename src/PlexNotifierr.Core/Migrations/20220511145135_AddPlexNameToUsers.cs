using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlexNotifierr.Core.Migrations
{
    public partial class AddPlexNameToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "plex_name",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plex_name",
                table: "users");
        }
    }
}
