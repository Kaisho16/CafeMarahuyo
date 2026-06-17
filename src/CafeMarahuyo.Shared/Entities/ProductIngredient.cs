using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("product_ingredients")]
    public class ProductIngredient
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("inventory_item_id")]
        public int InventoryItemId { get; set; }

        [Required]
        [Column("quantity_required", TypeName = "decimal(18,4)")]
        public decimal QuantityRequired { get; set; }

        public Product? Product { get; set; }
    }
}
