using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("promos")]
    public class Promo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column("discount_type")] // e.g., "percentage", "fixed"
        public string DiscountType { get; set; } = string.Empty;

        [Required]
        [Column("value", TypeName = "decimal(10,2)")]
        public decimal Value { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
