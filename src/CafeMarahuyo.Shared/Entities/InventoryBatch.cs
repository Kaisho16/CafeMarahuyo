using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("inventory_batches")]
    public class InventoryBatch
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("item_id")]
        public int InventoryItemId { get; set; }
        
        public InventoryItem? Item { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("expiration_date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
