using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CafeMarahuyo.Shared.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        
        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }
        
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        
        [JsonPropertyName("previous_quantity")]
        public int PreviousQuantity { get; set; }
        
        [JsonPropertyName("new_quantity")]
        public int NewQuantity { get; set; }
        
        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }
        
        public string? Notes { get; set; }
        
        [JsonPropertyName("performed_by")]
        public int PerformedBy { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("item_unit")]
        public string ItemUnit { get; set; } = string.Empty;

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("category_icon")]
        public string CategoryIcon { get; set; } = string.Empty;

        [JsonPropertyName("performed_by_name")]
        public string PerformedByName { get; set; } = string.Empty;
    }

    public class TransactionListResponse
    {
        public List<TransactionDto> Transactions { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}
