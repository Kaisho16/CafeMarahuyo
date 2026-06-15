using System;
using System.Collections.Generic;

namespace CafeMarahuyo.Shared.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class SizeModifierDto
    {
        public int Id { get; set; }
        public string SizeName { get; set; } = string.Empty;
        public decimal PriceModifier { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class AddOnDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class PosSettingsDto
    {
        public decimal TaxRate { get; set; }
        public string ReceiptFooter { get; set; } = string.Empty;
    }

    public class CreateOrderRequest
    {
        public string OrderType { get; set; } = "Dine-in";
        public string PaymentMode { get; set; } = string.Empty;
        public decimal AmountTendered { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public string? PromoCode { get; set; }
        public string? ReferenceCode { get; set; }
        public bool UseXendit { get; set; } = false;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Temperature { get; set; }
        public string? IceLevel { get; set; }
        public string? SugarLevel { get; set; }
        public List<CreateOrderItemAddOnRequest> AddOns { get; set; } = new();
    }

    public class CreateOrderItemAddOnRequest
    {
        public int AddOnId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public string? PromoCode { get; set; }
        public decimal PromoDiscount { get; set; }
        public decimal AmountTendered { get; set; }
        public decimal ChangeAmount { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string? ReceiptFooter { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        public string? Size { get; set; }
        public decimal SizeModifierPrice { get; set; }
        public string? Temperature { get; set; }
        public string? IceLevel { get; set; }
        public string? SugarLevel { get; set; }
        public string? CustomizationsJson { get; set; }
        public decimal AddonsTotal { get; set; }
        
        public decimal Subtotal { get; set; }
    }

    public class ValidatePromoRequest
    {
        public string PromoCode { get; set; } = string.Empty;
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateSizeModifierRequest
    {
        public decimal PriceModifier { get; set; }
    }

    public class CreateAddOnRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class UpdateAddOnRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class UpdatePosSettingsRequest
    {
        public decimal TaxRate { get; set; }
        public string ReceiptFooter { get; set; } = string.Empty;
    }

    public class SalesSummaryDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }

    public class ProductAuditLogDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
