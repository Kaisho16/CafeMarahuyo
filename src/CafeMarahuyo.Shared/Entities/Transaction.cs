using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("item_id")]
        public int ItemId { get; set; }
        
        public InventoryItem? Item { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("type")]
        public string Type { get; set; } = string.Empty; // 'stock_in', 'stock_out'

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("previous_quantity")]
        public int PreviousQuantity { get; set; }

        [Required]
        [Column("new_quantity")]
        public int NewQuantity { get; set; }

        [Column("expiration_date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("batch_id")]
        public int? BatchId { get; set; }

        [Required]
        [Column("performed_by")]
        public int PerformedBy { get; set; }

        public User? User { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
