using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [MaxLength(500)]
        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_available")]
        public bool IsAvailable { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductIngredient> Ingredients { get; set; } = new List<ProductIngredient>();
    }
}
