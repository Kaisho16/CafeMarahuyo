using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeMarahuyo.Shared.Migrations
{
    /// <inheritdoc />
    public partial class SupportDecimals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "previous_quantity",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "new_quantity",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "inventory_items",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "inventory_batches",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "previous_quantity",
                table: "transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "new_quantity",
                table: "transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "inventory_batches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");
        }
    }
}
