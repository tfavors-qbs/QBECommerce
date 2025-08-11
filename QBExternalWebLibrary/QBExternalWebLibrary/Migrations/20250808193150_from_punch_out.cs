using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class from_punch_out : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromId",
                table: "PunchOutSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromId",
                table: "PunchOutSessions");
        }
    }
}
