using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagaPatternDemo.Host.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderSagas",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymentProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ShippingProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSagas", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSagas_CreatedAt",
                table: "OrderSagas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSagas_State",
                table: "OrderSagas",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSagas");
        }
    }
}
