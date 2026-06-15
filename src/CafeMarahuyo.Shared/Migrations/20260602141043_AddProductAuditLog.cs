using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeMarahuyo.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_audit_logs");
        }
    }
}
