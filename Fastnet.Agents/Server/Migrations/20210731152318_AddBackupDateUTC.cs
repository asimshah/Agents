using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Agents.Server.Migrations
{
    public partial class AddBackupDateUTC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BackedUpOn",
                table: "Backups",
                newName: "BackupDateUTC");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BackedUpOnUTC",
                table: "Backups",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackedUpOnUTC",
                table: "Backups");

            migrationBuilder.RenameColumn(
                name: "BackupDateUTC",
                table: "Backups",
                newName: "BackedUpOn");
        }
    }
}
