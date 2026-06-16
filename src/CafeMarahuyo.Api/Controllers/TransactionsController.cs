using System;
using System.Linq;
using System.Threading.Tasks;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CafeMarahuyo.Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public TransactionsController(CafeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? item_id, 
            [FromQuery] string? category, 
            [FromQuery] string? type, 
            [FromQuery] string? date_from, 
            [FromQuery] string? date_to, 
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 25)
        {
            var query = _context.Transactions
                .Include(t => t.Item).ThenInclude(i => i!.Category)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(item_id) && int.TryParse(item_id, out int parsedItemId))
            {
                query = query.Where(t => t.ItemId == parsedItemId);
            }

            if (!string.IsNullOrEmpty(category) && category != "all" && int.TryParse(category, out int parsedCategoryId))
            {
                query = query.Where(t => t.Item!.CategoryId == parsedCategoryId);
            }

            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                query = query.Where(t => t.Type == type);
            }

            if (!string.IsNullOrEmpty(date_from) && DateTime.TryParse(date_from, out var dtFrom))
            {
                dtFrom = dtFrom.ToUniversalTime();
                query = query.Where(t => t.CreatedAt >= dtFrom);
            }

            if (!string.IsNullOrEmpty(date_to) && DateTime.TryParse(date_to, out var dtTo))
            {
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(t => t.CreatedAt <= dtTo);
            }

            var total = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var dtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                ItemId = t.ItemId,
                Type = t.Type,
                Quantity = t.Quantity,
                PreviousQuantity = t.PreviousQuantity,
                NewQuantity = t.NewQuantity,
                ExpirationDate = t.ExpirationDate?.ToString("yyyy-MM-dd"),
                Notes = t.Notes,
                PerformedBy = t.PerformedBy,
                CreatedAt = t.CreatedAt,
                ItemName = t.Item?.Name ?? "",
                ItemUnit = t.Item?.Unit ?? "",
                CategoryName = t.Item?.Category?.Name ?? "",
                CategoryIcon = t.Item?.Category?.Icon ?? "",
                PerformedByName = t.User?.DisplayName ?? ""
            }).ToList();

            return Ok(new TransactionListResponse
            {
                Transactions = dtos,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    Pages = (int)Math.Ceiling(total / (double)limit)
                }
            });
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] string? category, 
            [FromQuery] string? type, 
            [FromQuery] string? date_from, 
            [FromQuery] string? date_to)
        {
            var query = _context.Transactions
                .Include(t => t.Item).ThenInclude(i => i!.Category)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "all" && int.TryParse(category, out int parsedCategoryId))
            {
                query = query.Where(t => t.Item!.CategoryId == parsedCategoryId);
            }
            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                query = query.Where(t => t.Type == type);
            }
            var filename = "cafe_marahuyo_transactions";

            if (!string.IsNullOrEmpty(date_from) && DateTime.TryParse(date_from, out var dtFrom))
            {
                dtFrom = TimeZoneInfo.ConvertTimeToUtc(dtFrom, TimeZoneInfo.CreateCustomTimeZone("PH", TimeSpan.FromHours(8), "PH", "PH"));
                query = query.Where(t => t.CreatedAt >= dtFrom);
                filename += $"_from_{date_from}";
            }
            if (!string.IsNullOrEmpty(date_to) && DateTime.TryParse(date_to, out var dtTo))
            {
                dtTo = TimeZoneInfo.ConvertTimeToUtc(dtTo.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.CreateCustomTimeZone("PH", TimeSpan.FromHours(8), "PH", "PH"));
                query = query.Where(t => t.CreatedAt <= dtTo);
                filename += $"_to_{date_to}";
            }

            if (string.IsNullOrEmpty(date_from) && string.IsNullOrEmpty(date_to))
            {
                var timestamp = DateTime.UtcNow.AddHours(8).ToString("MM-dd-yyyy_hh-mm-tt");
                filename += $"_{timestamp}";
            }
            
            filename += ".csv";

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,Item,Category,Type,Quantity,Unit,Previous Qty,New Qty,Notes,Performed By");

            foreach (var t in transactions)
            {
                var typeLabel = t.Type == "stock_in" ? "Stock In" : "Stock Out";
                sb.AppendLine($"{t.Id},{t.CreatedAt.AddHours(8):yyyy-MM-ddTHH:mm:ss},\"{t.Item?.Name}\",\"{t.Item?.Category?.Name}\",{typeLabel},{t.Quantity},{t.Item?.Unit},{t.PreviousQuantity},{t.NewQuantity},\"{t.Notes?.Replace("\"", "\"\"")}\",\"{t.User?.DisplayName}\"");
            }

            Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
        }
    }
}
