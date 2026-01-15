using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBExternalWebLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddPastOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PastOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    PONumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastOrders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PastOrders_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PastOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PastOrderId = table.Column<int>(type: "int", nullable: false),
                    ContractItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastOrderItems_ContractItems_ContractItemId",
                        column: x => x.ContractItemId,
                        principalTable: "ContractItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PastOrderItems_PastOrders_PastOrderId",
                        column: x => x.PastOrderId,
                        principalTable: "PastOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PastOrderTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PastOrderId = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastOrderTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastOrderTags_PastOrders_PastOrderId",
                        column: x => x.PastOrderId,
                        principalTable: "PastOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PastOrderItems_ContractItemId",
                table: "PastOrderItems",
                column: "ContractItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PastOrderItems_PastOrderId",
                table: "PastOrderItems",
                column: "PastOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PastOrders_ClientId",
                table: "PastOrders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PastOrders_UserId",
                table: "PastOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PastOrderTags_PastOrderId",
                table: "PastOrderTags",
                column: "PastOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PastOrderItems");

            migrationBuilder.DropTable(
                name: "PastOrderTags");

            migrationBuilder.DropTable(
                name: "PastOrders");
        }
    }
}
