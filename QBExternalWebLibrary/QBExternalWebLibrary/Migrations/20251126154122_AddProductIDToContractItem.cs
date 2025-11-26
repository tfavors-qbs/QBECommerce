using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIDToContractItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductIDId",
                table: "ContractItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_ProductIDId",
                table: "ContractItems",
                column: "ProductIDId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractItems_ProductIDs_ProductIDId",
                table: "ContractItems",
                column: "ProductIDId",
                principalTable: "ProductIDs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractItems_ProductIDs_ProductIDId",
                table: "ContractItems");

            migrationBuilder.DropIndex(
                name: "IX_ContractItems_ProductIDId",
                table: "ContractItems");

            migrationBuilder.DropColumn(
                name: "ProductIDId",
                table: "ContractItems");
        }
    }
}
