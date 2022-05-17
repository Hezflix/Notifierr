using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlexNotifierr.Core.Migrations
{
    public partial class ChangeMediaFieldAndAddHistoryPositionToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grand_parent_rating_key",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "media_index",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "media_type",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "parent_media_index",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "parent_rating_key",
                table: "medias");

            migrationBuilder.AddColumn<int>(
                name: "history_position",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_notified",
                table: "medias",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "medias",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "thumb",
                table: "medias",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "history_position",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_notified",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "summary",
                table: "medias");

            migrationBuilder.DropColumn(
                name: "thumb",
                table: "medias");

            migrationBuilder.AddColumn<int>(
                name: "grand_parent_rating_key",
                table: "medias",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "media_index",
                table: "medias",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "media_type",
                table: "medias",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "parent_media_index",
                table: "medias",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_rating_key",
                table: "medias",
                type: "INTEGER",
                nullable: true);
        }
    }
}
