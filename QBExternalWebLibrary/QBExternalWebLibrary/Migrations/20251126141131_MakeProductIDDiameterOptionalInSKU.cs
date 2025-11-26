using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductIDDiameterOptionalInSKU : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SKUs_Diameters_DiameterId",
                table: "SKUs");

            migrationBuilder.DropForeignKey(
                name: "FK_SKUs_ProductIDs_ProductIDId",
                table: "SKUs");

            migrationBuilder.AlterColumn<int>(
                name: "ProductIDId",
                table: "SKUs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DiameterId",
                table: "SKUs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SKUs_Diameters_DiameterId",
                table: "SKUs",
                column: "DiameterId",
                principalTable: "Diameters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SKUs_ProductIDs_ProductIDId",
                table: "SKUs",
                column: "ProductIDId",
                principalTable: "ProductIDs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SKUs_Diameters_DiameterId",
                table: "SKUs");

            migrationBuilder.DropForeignKey(
                name: "FK_SKUs_ProductIDs_ProductIDId",
                table: "SKUs");

            migrationBuilder.AlterColumn<int>(
                name: "ProductIDId",
                table: "SKUs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DiameterId",
                table: "SKUs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SKUs_Diameters_DiameterId",
                table: "SKUs",
                column: "DiameterId",
                principalTable: "Diameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SKUs_ProductIDs_ProductIDId",
                table: "SKUs",
                column: "ProductIDId",
                principalTable: "ProductIDs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
