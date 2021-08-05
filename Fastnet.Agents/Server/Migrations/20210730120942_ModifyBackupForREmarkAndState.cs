using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Agents.Server.Migrations
{
    public partial class ModifyBackupForREmarkAndState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Backups",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Backups");
        }
    }
}
