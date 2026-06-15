using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeMarahuyo.Shared.Entities
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("order_number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("total_amount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("payment_mode")] // "Cash", "Digital Cash"
        public string PaymentMode { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("promo_code")]
        public string? PromoCode { get; set; }

        [Column("promo_discount", TypeName = "decimal(10,2)")]
        public decimal PromoDiscount { get; set; } = 0;

        [Column("cashier_name")]
        public string CashierName { get; set; } = "Unknown";

        [Column("order_type")]
        public string OrderType { get; set; } = "Dine-in";  // "Dine-in" or "Takeout"

        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column("tax_amount", TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }

        [Column("discount_type")]
        public string? DiscountType { get; set; }  // "percentage" or "fixed"

        [Column("discount_value", TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Column("amount_tendered", TypeName = "decimal(10,2)")]
        public decimal AmountTendered { get; set; }

        [Column("change_amount", TypeName = "decimal(10,2)")]
        public decimal ChangeAmount { get; set; }

        [Column("receipt_footer")]
        public string? ReceiptFooter { get; set; }

        [Column("payment_status")]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Completed";

        [Column("payment_reference")]
        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        [Column("payment_url")]
        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
