using System.Collections.Generic;

namespace CafeMarahuyo.Shared.DTOs
{
    public class DashboardSummaryResponse
    {
        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalValue { get; set; }
        public int TodayStockIn { get; set; }
        public int TodayStockOut { get; set; }
        public int TodayTransactions { get; set; }
    }

    public class UsageChartResponse
    {
        public List<string> Labels { get; set; } = new();
        public List<int> StockInData { get; set; } = new();
        public List<int> StockOutData { get; set; } = new();
    }

    public class CategoryBreakdownResponse
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
        public List<int> Counts { get; set; } = new();
        public List<string> Icons { get; set; } = new();
    }
}
