using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Agents.Server.Migrations
{
    public partial class ModifyBackupPathColumnName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullPath",
                table: "Backups",
                newName: "FullFilename");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullFilename",
                table: "Backups",
                newName: "FullPath");
        }
    }
}
