using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeMarahuyo.Shared.Migrations.PosDb
{
    /// <inheritdoc />
    public partial class AddProductIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "add_ons",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_add_ons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    order_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    payment_mode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    promo_code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    promo_discount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    cashier_name = table.Column<string>(type: "TEXT", nullable: false),
                    order_type = table.Column<string>(type: "TEXT", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    discount_type = table.Column<string>(type: "TEXT", nullable: true),
                    discount_value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    amount_tendered = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    change_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    receipt_footer = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    product_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    performed_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    category_name = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    image_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    is_available = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promos",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    discount_type = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    valid_until = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "size_modifiers",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    size_name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    price_modifier = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_size_modifiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    size = table.Column<string>(type: "TEXT", nullable: true),
                    size_modifier_price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    temperature = table.Column<string>(type: "TEXT", nullable: true),
                    ice_level = table.Column<string>(type: "TEXT", nullable: true),
                    sugar_level = table.Column<string>(type: "TEXT", nullable: true),
                    customizations_json = table.Column<string>(type: "TEXT", nullable: true),
                    addons_total = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_ingredients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    inventory_item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    quantity_required = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_ingredients_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_date",
                table: "orders",
                column: "order_date");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_settings_key",
                table: "pos_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ingredients_product_id",
                table: "product_ingredients",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                table: "products",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promos_code",
                table: "promos",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "add_ons");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "pos_settings");

            migrationBuilder.DropTable(
                name: "product_audit_logs");

            migrationBuilder.DropTable(
                name: "product_ingredients");

            migrationBuilder.DropTable(
                name: "promos");

            migrationBuilder.DropTable(
                name: "size_modifiers");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
