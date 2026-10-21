using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products",
                sql: "[Price] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products",
                sql: "[Stock] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems",
                sql: "[UnitPrice] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems");
        }
    }
}
