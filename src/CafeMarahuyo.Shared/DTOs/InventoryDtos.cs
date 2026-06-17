using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CafeMarahuyo.Shared.DTOs
{
    public class InventoryBatchDto
    {
        public int Id { get; set; }
        
        [JsonPropertyName("inventory_item_id")]
        public int InventoryItemId { get; set; }
        
        public decimal Quantity { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }
    }

    public class InventoryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }
        
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        
        [JsonPropertyName("cost_per_unit")]
        public decimal CostPerUnit { get; set; }
        
        [JsonPropertyName("min_stock_level")]
        public int MinStockLevel { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; } // yyyy-MM-dd
        
        public string? Description { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("category_icon")]
        public string CategoryIcon { get; set; } = string.Empty;

        public List<InventoryBatchDto> Batches { get; set; } = new();
    }

    public class InventoryListResponse
    {
        public List<InventoryItemDto> Items { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class PaginationDto
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int Pages { get; set; }
    }

    public class CreateInventoryItemRequest
    {
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }
        
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        
        [JsonPropertyName("cost_per_unit")]
        public decimal CostPerUnit { get; set; }
        
        [JsonPropertyName("min_stock_level")]
        public int MinStockLevel { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }
        
        public string? Description { get; set; }
    }

    public class UpdateInventoryItemRequest
    {
        public string? Name { get; set; }
        
        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }
        
        public string? Unit { get; set; }
        
        [JsonPropertyName("cost_per_unit")]
        public decimal? CostPerUnit { get; set; }
        
        [JsonPropertyName("min_stock_level")]
        public int? MinStockLevel { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }
        
        public string? Description { get; set; }
    }

    public class StockAdjustmentRequest
    {
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }
    }
}
