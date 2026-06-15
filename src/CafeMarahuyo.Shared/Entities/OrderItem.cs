using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("order_id")]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("unit_price", TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column("size")]
        public string? Size { get; set; } // "Small", "Medium", "Large"

        [Column("size_modifier_price", TypeName = "decimal(10,2)")]
        public decimal SizeModifierPrice { get; set; }

        [Column("temperature")]
        public string? Temperature { get; set; } // "Hot", "Iced"

        [Column("ice_level")]
        public string? IceLevel { get; set; } // "No Ice", "Less Ice", "Regular Ice", "Extra Ice"

        [Column("sugar_level")]
        public string? SugarLevel { get; set; } // "No Sugar", "Less Sugar", "Regular", "Extra Sugar"

        [Column("customizations_json")]
        public string? CustomizationsJson { get; set; } // JSON array of selected add-ons

        [Column("addons_total", TypeName = "decimal(10,2)")]
        public decimal AddonsTotal { get; set; }
    }
}
