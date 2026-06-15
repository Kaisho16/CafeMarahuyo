using System;
using System.Linq;
using System.Threading.Tasks;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.DTOs;
using CafeMarahuyo.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace CafeMarahuyo.Api.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public InventoryController(CafeDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return Ok(categories);
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] string? date_from, 
            [FromQuery] string? date_to)
        {
            var query = _context.InventoryItems
                .Include(i => i.Category)
                .Include(i => i.Batches)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date_from) && DateTime.TryParse(date_from, out var dtFrom))
            {
                dtFrom = dtFrom.ToUniversalTime();
                query = query.Where(i => i.UpdatedAt >= dtFrom);
            }

            if (!string.IsNullOrEmpty(date_to) && DateTime.TryParse(date_to, out var dtTo))
            {
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(i => i.UpdatedAt <= dtTo);
            }

            var items = await query
                .OrderBy(i => i.Category!.Name).ThenBy(i => i.Name)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Name,Category,Quantity,Unit,Cost Per Unit,Min Stock Level,Status,Earliest Expiration Date,Description,Last Updated");

            foreach (var item in items)
            {
                var status = item.Quantity < item.MinStockLevel ? "Low Stock" : "In Stock";
                var earliestExp = item.Batches.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate;
                var exp = earliestExp?.ToString("yyyy-MM-dd") ?? "N/A";
                
                sb.AppendLine($"{item.Id},\"{item.Name}\",\"{item.Category?.Name}\",{item.Quantity},{item.Unit},{item.CostPerUnit},{item.MinStockLevel},{status},{exp},\"{item.Description?.Replace("\"", "\"\"")}\",{item.UpdatedAt:yyyy-MM-ddTHH:mm:ss}");
            }

            var timestamp = DateTime.UtcNow.AddHours(8).ToString("MM-dd-yyyy_hh-mm-tt");
            var filename = $"cafe_marahuyo_inventory_{timestamp}.csv";
            Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory([FromQuery] string? search, [FromQuery] string? category, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string sort = "name", [FromQuery] string order = "asc")
        {
            var query = _context.InventoryItems
                .Include(i => i.Category)
                .Include(i => i.Batches)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => EF.Functions.Like(i.Name, $"%{search}%") || EF.Functions.Like(i.Description!, $"%{search}%"));
            }

            if (!string.IsNullOrEmpty(category) && category != "all" && int.TryParse(category, out int catId))
            {
                query = query.Where(i => i.CategoryId == catId);
            }

            if (status == "low")
            {
                query = query.Where(i => i.Quantity < i.MinStockLevel);
            }
            else if (status == "ok")
            {
                query = query.Where(i => i.Quantity >= i.MinStockLevel);
            }

            var total = await query.CountAsync();

            query = sort.ToLower() switch
            {
                "quantity" => order == "desc" ? query.OrderByDescending(i => i.Quantity) : query.OrderBy(i => i.Quantity),
                "cost_per_unit" => order == "desc" ? query.OrderByDescending(i => i.CostPerUnit) : query.OrderBy(i => i.CostPerUnit),
                "updated_at" => order == "desc" ? query.OrderByDescending(i => i.UpdatedAt) : query.OrderBy(i => i.UpdatedAt),
                "category_name" => order == "desc" ? query.OrderByDescending(i => i.Category!.Name) : query.OrderBy(i => i.Category!.Name),
                // Expiration date sorting is tricky with batches, we'll fallback to name
                _ => order == "desc" ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name)
            };

            var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

            var dtos = items.Select(i => new InventoryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                CategoryId = i.CategoryId,
                Quantity = i.Quantity,
                Unit = i.Unit,
                CostPerUnit = i.CostPerUnit,
                MinStockLevel = i.MinStockLevel,
                ExpirationDate = i.Batches.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                Description = i.Description,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                CategoryName = i.Category?.Name ?? "",
                CategoryIcon = i.Category?.Icon ?? "",
                Batches = i.Batches.Select(b => new InventoryBatchDto 
                {
                    Id = b.Id,
                    InventoryItemId = b.InventoryItemId,
                    Quantity = b.Quantity,
                    ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                }).ToList()
            }).ToList();

            // Handle manual sorting for expiration date since it's computed
            if (sort.ToLower() == "expiration_date")
            {
                if (order == "desc")
                    dtos = dtos.OrderByDescending(d => d.ExpirationDate ?? "9999-12-31").ToList();
                else
                    dtos = dtos.OrderBy(d => d.ExpirationDate ?? "9999-12-31").ToList();
            }

            return Ok(new InventoryListResponse
            {
                Items = dtos,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    Pages = (int)Math.Ceiling(total / (double)limit)
                }
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Category)
                .Include(i => i.Batches)
                .FirstOrDefaultAsync(i => i.Id == id);
                
            if (item == null) return NotFound(new { error = "Item not found" });

            return Ok(new InventoryItemDto
            {
                Id = item.Id,
                Name = item.Name,
                CategoryId = item.CategoryId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                CostPerUnit = item.CostPerUnit,
                MinStockLevel = item.MinStockLevel,
                ExpirationDate = item.Batches.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                Description = item.Description,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                CategoryName = item.Category?.Name ?? "",
                CategoryIcon = item.Category?.Icon ?? "",
                Batches = item.Batches.Select(b => new InventoryBatchDto 
                {
                    Id = b.Id,
                    InventoryItemId = b.InventoryItemId,
                    Quantity = b.Quantity,
                    ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                }).ToList()
            });
        }

        [HttpPost]
        [Authorize(Roles = "admin,Inventory Manager")]
        public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemRequest req)
        {
            if (string.IsNullOrEmpty(req.Name) || req.CategoryId <= 0 || string.IsNullOrEmpty(req.Unit))
            {
                return BadRequest(new { error = "Name, category, unit, and cost are required" });
            }

            DateTime? expDate = null;
            if (!string.IsNullOrEmpty(req.ExpirationDate) && DateTime.TryParse(req.ExpirationDate, out var parsedDate))
            {
                expDate = parsedDate.ToUniversalTime();
            }

            if (await _context.InventoryItems.AnyAsync(i => i.Name.ToLower() == req.Name.ToLower()))
            {
                return Conflict(new { error = $"An item with the name '{req.Name}' already exists." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = new InventoryItem
                {
                    Name = req.Name,
                    CategoryId = req.CategoryId,
                    Quantity = req.Quantity,
                    Unit = req.Unit,
                    CostPerUnit = req.CostPerUnit,
                    MinStockLevel = req.MinStockLevel > 0 ? req.MinStockLevel : 5,
                    Description = req.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.InventoryItems.Add(item);
                await _context.SaveChangesAsync();
                
                InventoryBatch? batch = null;
                if (req.Quantity > 0)
                {
                    batch = new InventoryBatch
                    {
                        InventoryItemId = item.Id,
                        Quantity = req.Quantity,
                        ExpirationDate = expDate
                    };
                    _context.InventoryBatches.Add(batch);
                    await _context.SaveChangesAsync();
                }

                var userId = int.Parse(User.FindFirstValue("id")!);
                _context.Transactions.Add(new Transaction
                {
                    ItemId = item.Id,
                    BatchId = batch?.Id,
                    Type = "stock_in",
                    Quantity = req.Quantity,
                    PreviousQuantity = 0,
                    NewQuantity = req.Quantity,
                    Notes = "Initial stock entry",
                    PerformedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                await _context.Entry(item).Reference(i => i.Category).LoadAsync();
                if (batch != null)
                {
                    await _context.Entry(item).Collection(i => i.Batches).LoadAsync();
                }

                return StatusCode(201, new InventoryItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    CostPerUnit = item.CostPerUnit,
                    MinStockLevel = item.MinStockLevel,
                    ExpirationDate = item.Batches?.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                    Description = item.Description,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    CategoryName = item.Category?.Name ?? "",
                    CategoryIcon = item.Category?.Icon ?? "",
                    Batches = item.Batches?.Select(b => new InventoryBatchDto 
                    {
                        Id = b.Id,
                        InventoryItemId = b.InventoryItemId,
                        Quantity = b.Quantity,
                        ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                    }).ToList() ?? new System.Collections.Generic.List<InventoryBatchDto>()
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,Inventory Manager")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateInventoryItemRequest req)
        {
            var item = await _context.InventoryItems.Include(i => i.Category).Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound(new { error = "Item not found" });

            if (req.Name != null) item.Name = req.Name;
            if (req.CategoryId.HasValue) item.CategoryId = req.CategoryId.Value;
            if (req.Unit != null) item.Unit = req.Unit;
            if (req.CostPerUnit.HasValue) item.CostPerUnit = req.CostPerUnit.Value;
            if (req.MinStockLevel.HasValue) item.MinStockLevel = req.MinStockLevel.Value;
            if (req.Description != null) item.Description = req.Description;

            // Note: Expiration date is no longer updated directly on the item here,
            // as it belongs to batches. Modifying existing batches would require a specific batch endpoint.

            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _context.Entry(item).Reference(i => i.Category).LoadAsync();

            return Ok(new InventoryItemDto
            {
                Id = item.Id,
                Name = item.Name,
                CategoryId = item.CategoryId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                CostPerUnit = item.CostPerUnit,
                MinStockLevel = item.MinStockLevel,
                ExpirationDate = item.Batches.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                Description = item.Description,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                CategoryName = item.Category?.Name ?? "",
                CategoryIcon = item.Category?.Icon ?? "",
                Batches = item.Batches.Select(b => new InventoryBatchDto 
                {
                    Id = b.Id,
                    InventoryItemId = b.InventoryItemId,
                    Quantity = b.Quantity,
                    ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                }).ToList()
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,Inventory Manager")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound(new { error = "Item not found" });

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item deleted successfully" });
        }

        [HttpPost("{id}/stock-in")]
        public async Task<IActionResult> StockIn(int id, [FromBody] StockAdjustmentRequest req)
        {
            if (req.Quantity <= 0) return BadRequest(new { error = "Valid whole number quantity is required" });

            var item = await _context.InventoryItems.Include(i => i.Category).Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound(new { error = "Item not found" });

            var userId = int.Parse(User.FindFirstValue("id")!);
            var previousQty = item.Quantity;
            var newQty = previousQty + req.Quantity;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                item.Quantity = newQty;
                item.UpdatedAt = DateTime.UtcNow;
                
                DateTime? expDate = null;
                if (!string.IsNullOrEmpty(req.ExpirationDate) && DateTime.TryParse(req.ExpirationDate, out var pDate))
                {
                    expDate = pDate.ToUniversalTime().Date;
                }

                // Check if a batch with this EXACT expiration date already exists
                var batch = item.Batches.FirstOrDefault(b => 
                    (b.ExpirationDate.HasValue && expDate.HasValue && b.ExpirationDate.Value.Date == expDate.Value) || 
                    (!b.ExpirationDate.HasValue && !expDate.HasValue));
                
                if (batch != null)
                {
                    batch.Quantity += req.Quantity;
                    batch.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    batch = new InventoryBatch
                    {
                        InventoryItemId = id,
                        Quantity = req.Quantity,
                        ExpirationDate = expDate
                    };
                    _context.InventoryBatches.Add(batch);
                }

                await _context.SaveChangesAsync();

                _context.Transactions.Add(new Transaction
                {
                    ItemId = id,
                    BatchId = batch.Id,
                    Type = "stock_in",
                    Quantity = req.Quantity,
                    PreviousQuantity = previousQty,
                    NewQuantity = newQty,
                    Notes = req.Notes ?? "",
                    PerformedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new InventoryItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    CostPerUnit = item.CostPerUnit,
                    MinStockLevel = item.MinStockLevel,
                    ExpirationDate = item.Batches.Where(b => b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                    Description = item.Description,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    CategoryName = item.Category?.Name ?? "",
                    CategoryIcon = item.Category?.Icon ?? "",
                    Batches = item.Batches.Select(b => new InventoryBatchDto 
                    {
                        Id = b.Id,
                        InventoryItemId = b.InventoryItemId,
                        Quantity = b.Quantity,
                        ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                    }).ToList()
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Server error" });
            }
        }

        [HttpPost("{id}/stock-out")]
        public async Task<IActionResult> StockOut(int id, [FromBody] StockAdjustmentRequest req)
        {
            if (req.Quantity <= 0) return BadRequest(new { error = "Valid whole number quantity is required" });

            var item = await _context.InventoryItems.Include(i => i.Category).Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound(new { error = "Item not found" });

            if (req.Quantity > item.Quantity) return BadRequest(new { error = "Insufficient stock" });

            var userId = int.Parse(User.FindFirstValue("id")!);
            var previousQty = item.Quantity;
            var newQty = previousQty - req.Quantity;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                item.Quantity = newQty;
                item.UpdatedAt = DateTime.UtcNow;

                int remainingToDeduct = req.Quantity;
                
                // Sort batches: Earliest expiring first. Null expirations go last.
                var orderedBatches = item.Batches
                    .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)
                    .ThenBy(b => b.ExpirationDate)
                    .ToList();

                foreach (var batch in orderedBatches)
                {
                    if (remainingToDeduct <= 0) break;

                    if (batch.Quantity > 0)
                    {
                        int deductAmt = Math.Min(batch.Quantity, remainingToDeduct);
                        batch.Quantity -= deductAmt;
                        batch.UpdatedAt = DateTime.UtcNow;
                        remainingToDeduct -= deductAmt;
                        
                        // We could log a transaction PER BATCH if we wanted, but to keep it simple,
                        // we'll log one master transaction, or log multiple if we really need to track.
                        // Let's log one transaction but we can't easily link it to multiple BatchIds.
                        // For now, we'll link it to the PRIMARY batch we deducted from (the first one).
                    }
                }

                await _context.SaveChangesAsync();

                _context.Transactions.Add(new Transaction
                {
                    ItemId = id,
                    Type = "stock_out",
                    Quantity = req.Quantity,
                    PreviousQuantity = previousQty,
                    NewQuantity = newQty,
                    Notes = req.Notes ?? "FIFO Deduction",
                    PerformedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                
                // Clean up empty batches
                var emptyBatches = item.Batches.Where(b => b.Quantity == 0).ToList();
                _context.InventoryBatches.RemoveRange(emptyBatches);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new InventoryItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    CostPerUnit = item.CostPerUnit,
                    MinStockLevel = item.MinStockLevel,
                    ExpirationDate = item.Batches.Where(b => b.Quantity > 0 && b.ExpirationDate.HasValue).OrderBy(b => b.ExpirationDate).FirstOrDefault()?.ExpirationDate?.ToString("yyyy-MM-dd"),
                    Description = item.Description,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    CategoryName = item.Category?.Name ?? "",
                    CategoryIcon = item.Category?.Icon ?? "",
                    Batches = item.Batches.Where(b => b.Quantity > 0).Select(b => new InventoryBatchDto 
                    {
                        Id = b.Id,
                        InventoryItemId = b.InventoryItemId,
                        Quantity = b.Quantity,
                        ExpirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd")
                    }).ToList()
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Server error" });
            }
        }
    }
}
