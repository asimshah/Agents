using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Agents.Server.Migrations
{
    public partial class AddWebsiteContentRoot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentRoot",
                table: "BackupSourceFolders",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentRoot",
                table: "BackupSourceFolders");
        }
    }
}
