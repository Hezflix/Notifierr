using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlexNotifierr.Core.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medias",
                columns: table => new
                {
                    rating_key = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    media_index = table.Column<int>(type: "INTEGER", nullable: true),
                    media_type = table.Column<int>(type: "INTEGER", nullable: false),
                    parent_rating_key = table.Column<int>(type: "INTEGER", nullable: true),
                    parent_media_index = table.Column<int>(type: "INTEGER", nullable: true),
                    grand_parent_rating_key = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medias", x => x.rating_key);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    plex_id = table.Column<int>(type: "INTEGER", nullable: false),
                    active = table.Column<bool>(type: "INTEGER", nullable: false),
                    discord_id = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    rating_key = table.Column<int>(type: "INTEGER", nullable: false),
                    active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => new { x.user_id, x.rating_key });
                    table.ForeignKey(
                        name: "FK_user_subscriptions_medias_rating_key",
                        column: x => x.rating_key,
                        principalTable: "medias",
                        principalColumn: "rating_key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_rating_key",
                table: "user_subscriptions",
                column: "rating_key");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "medias");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
