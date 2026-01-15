using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToQuickOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "QuickOrders",
                type: "int",
                nullable: true);

            // Populate ClientId for existing QuickOrders from Owner's ClientId
            migrationBuilder.Sql(@"
                UPDATE QuickOrders
                SET ClientId = u.ClientId
                FROM QuickOrders q
                INNER JOIN AspNetUsers u ON q.OwnerId = u.Id
                WHERE q.ClientId IS NULL AND u.ClientId IS NOT NULL
            ");

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrders_ClientId",
                table: "QuickOrders",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuickOrders_Clients_ClientId",
                table: "QuickOrders",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuickOrders_Clients_ClientId",
                table: "QuickOrders");

            migrationBuilder.DropIndex(
                name: "IX_QuickOrders_ClientId",
                table: "QuickOrders");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "QuickOrders");
        }
    }
}
