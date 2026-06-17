using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("inventory_items")]
    public class InventoryItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }
        
        public Category? Category { get; set; }
        
        public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();

        [Required]
        [Column("quantity", TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        [Column("unit")]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [Column("cost_per_unit", TypeName = "decimal(10,2)")]
        public decimal CostPerUnit { get; set; } = 0;

        [Required]
        [Column("min_stock_level")]
        public int MinStockLevel { get; set; } = 5;


        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
