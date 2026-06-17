using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeMarahuyo.Shared.Migrations.PosDb
{
    /// <inheritdoc />
    public partial class SupportDecimalsPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "quantity_required",
                table: "product_ingredients",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "quantity_required",
                table: "product_ingredients",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");
        }
    }
}
