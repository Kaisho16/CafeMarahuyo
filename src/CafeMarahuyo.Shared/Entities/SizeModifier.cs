using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("size_modifiers")]
    public class SizeModifier
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("size_name")]
        public string SizeName { get; set; } = string.Empty; // e.g., "Small", "Medium", "Large"

        [Required]
        [Column("price_modifier", TypeName = "decimal(10,2)")]
        public decimal PriceModifier { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
