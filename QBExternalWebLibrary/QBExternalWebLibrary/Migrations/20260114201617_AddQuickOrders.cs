using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuickOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsSharedClientWide = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickOrders_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuickOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuickOrderId = table.Column<int>(type: "int", nullable: false),
                    ContractItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickOrderItems_ContractItems_ContractItemId",
                        column: x => x.ContractItemId,
                        principalTable: "ContractItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuickOrderItems_QuickOrders_QuickOrderId",
                        column: x => x.QuickOrderId,
                        principalTable: "QuickOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuickOrderTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuickOrderId = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickOrderTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickOrderTags_QuickOrders_QuickOrderId",
                        column: x => x.QuickOrderId,
                        principalTable: "QuickOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrderItems_ContractItemId",
                table: "QuickOrderItems",
                column: "ContractItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrderItems_QuickOrderId",
                table: "QuickOrderItems",
                column: "QuickOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrders_OwnerId",
                table: "QuickOrders",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrderTags_QuickOrderId",
                table: "QuickOrderTags",
                column: "QuickOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuickOrderItems");

            migrationBuilder.DropTable(
                name: "QuickOrderTags");

            migrationBuilder.DropTable(
                name: "QuickOrders");
        }
    }
}
