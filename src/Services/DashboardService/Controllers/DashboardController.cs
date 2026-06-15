using System;
using System.Linq;
using System.Threading.Tasks;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public DashboardController(CafeDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalItems = await _context.InventoryItems.CountAsync();
            var lowStockCount = await _context.InventoryItems.CountAsync(i => i.Quantity < i.MinStockLevel);
            var totalValue = await _context.InventoryItems.SumAsync(i => (decimal)i.Quantity * i.CostPerUnit);

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todayStockIn = await _context.Transactions
                .Where(t => t.Type == "stock_in" && t.CreatedAt >= today && t.CreatedAt < tomorrow)
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            var todayStockOut = await _context.Transactions
                .Where(t => t.Type == "stock_out" && t.CreatedAt >= today && t.CreatedAt < tomorrow)
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            var todayTransactions = await _context.Transactions
                .CountAsync(t => t.CreatedAt >= today && t.CreatedAt < tomorrow);

            return Ok(new DashboardSummaryResponse
            {
                TotalItems = totalItems,
                LowStockCount = lowStockCount,
                TotalValue = totalValue,
                TodayStockIn = todayStockIn,
                TodayStockOut = todayStockOut,
                TodayTransactions = todayTransactions
            });
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _context.InventoryItems
                .Include(i => i.Category)
                .Include(i => i.Batches)
                .Where(i => i.Quantity < i.MinStockLevel)
                .ToListAsync();

            var sortedItems = items
                .OrderBy(i => i.MinStockLevel > 0 ? (double)i.Quantity / i.MinStockLevel : 0)
                .Select(i => new InventoryItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    CategoryId = i.CategoryId,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    CostPerUnit = i.CostPerUnit,
                    MinStockLevel = i.MinStockLevel,
                    ExpirationDate = i.Batches?.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                    Description = i.Description,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    CategoryName = i.Category?.Name ?? "",
                    CategoryIcon = i.Category?.Icon ?? ""
                })
                .ToList();

            return Ok(sortedItems);
        }

        [HttpGet("usage-chart")]
        public async Task<IActionResult> GetUsageChart()
        {
            var response = new UsageChartResponse();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var tomorrow = date.AddDays(1);

                response.Labels.Add(date.ToString("ddd"));

                var stockIn = await _context.Transactions
                    .Where(t => t.Type == "stock_in" && t.CreatedAt >= date && t.CreatedAt < tomorrow)
                    .SumAsync(t => (int?)t.Quantity) ?? 0;

                var stockOut = await _context.Transactions
                    .Where(t => t.Type == "stock_out" && t.CreatedAt >= date && t.CreatedAt < tomorrow)
                    .SumAsync(t => (int?)t.Quantity) ?? 0;

                response.StockInData.Add(stockIn);
                response.StockOutData.Add(stockOut);
            }

            return Ok(response);
        }

        [HttpGet("category-breakdown")]
        public async Task<IActionResult> GetCategoryBreakdown()
        {
            var categories = await _context.Categories.ToListAsync();
            var items = await _context.InventoryItems.ToListAsync();

            var grouped = categories.GroupJoin(
                items,
                c => c.Id,
                i => i.CategoryId,
                (c, inv) => new
                {
                    Name = c.Name,
                    Icon = c.Icon ?? "inventory_2",
                    ItemCount = inv.Count(),
                    TotalValue = inv.Sum(i => (decimal)i.Quantity * i.CostPerUnit)
                }
            ).OrderByDescending(x => x.TotalValue).ToList();

            var response = new CategoryBreakdownResponse
            {
                Labels = grouped.Select(g => g.Name).ToList(),
                Values = grouped.Select(g => g.TotalValue).ToList(),
                Counts = grouped.Select(g => g.ItemCount).ToList(),
                Icons = grouped.Select(g => g.Icon).ToList()
            };

            return Ok(response);
        }
    }
}
