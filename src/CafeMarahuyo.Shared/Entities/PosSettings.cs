using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("pos_settings")]
    public class PosSettings
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("key")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [Column("value")]
        public string Value { get; set; } = string.Empty;
    }
}
