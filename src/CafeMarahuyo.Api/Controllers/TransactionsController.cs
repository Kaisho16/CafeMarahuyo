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

        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportExcel(
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
            
            filename += ".xlsx";

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Transactions");
            
            var headers = new[] { "ID", "Date", "Item", "Category", "Type", "Quantity", "Unit", "Previous Qty", "New Qty", "Notes", "Performed By" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            int row = 2;
            foreach (var t in transactions)
            {
                ws.Cell(row, 1).Value = t.Id;
                ws.Cell(row, 2).Value = t.CreatedAt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(row, 3).Value = t.Item?.Name;
                ws.Cell(row, 4).Value = t.Item?.Category?.Name;
                ws.Cell(row, 5).Value = t.Type;
                
                if (t.Type == "stock_in")
                {
                    ws.Cell(row, 5).Style.Font.FontColor = ClosedXML.Excel.XLColor.Green;
                }
                else if (t.Type == "stock_out")
                {
                    ws.Cell(row, 5).Style.Font.FontColor = ClosedXML.Excel.XLColor.Red;
                }

                ws.Cell(row, 6).Value = t.Quantity;
                ws.Cell(row, 7).Value = t.Item?.Unit;
                ws.Cell(row, 8).Value = t.PreviousQuantity;
                ws.Cell(row, 9).Value = t.NewQuantity;
                ws.Cell(row, 10).Value = t.Notes;
                ws.Cell(row, 11).Value = t.User?.DisplayName;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
    }
}
