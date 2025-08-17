using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagaPatternDemo.Host.Migrations
{
    /// <inheritdoc />
    public partial class InitialSQLiteCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderSagas",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShippingProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
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
