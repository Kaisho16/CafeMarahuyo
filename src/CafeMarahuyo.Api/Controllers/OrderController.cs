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
using System.Collections.Generic;
using System.Text.Json;

namespace CafeMarahuyo.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly PosDbContext _context;
        private readonly CafeDbContext _cafeContext;
        private readonly CafeMarahuyo.Api.Services.IInventoryManager _inventoryManager;

        public OrderController(PosDbContext context, CafeDbContext cafeContext, CafeMarahuyo.Api.Services.IInventoryManager inventoryManager)
        {
            _context = context;
            _cafeContext = cafeContext;
            _inventoryManager = inventoryManager;
        }

        // ==========================================
        // 1. PRODUCTS & CUSTOMIZATIONS
        // ==========================================

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.CategoryName)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var dtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.CategoryName,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Description = p.Description,
                IsAvailable = p.IsAvailable
            });

            return Ok(dtos);
        }

        [HttpGet("size-modifiers")]
        public async Task<IActionResult> GetActiveSizeModifiers()
        {
            var sizes = await _context.SizeModifiers
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new SizeModifierDto
                {
                    Id = s.Id,
                    SizeName = s.SizeName,
                    PriceModifier = s.PriceModifier,
                    IsActive = s.IsActive,
                    SortOrder = s.SortOrder
                }).ToListAsync();
            return Ok(sizes);
        }

        [HttpGet("addons")]
        public async Task<IActionResult> GetActiveAddOns()
        {
            var addons = await _context.AddOns
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.SortOrder)
                .Select(a => new AddOnDto
                {
                    Id = a.Id,
                    Category = a.Category,
                    Name = a.Name,
                    Price = a.Price,
                    IsActive = a.IsActive,
                    SortOrder = a.SortOrder
                }).ToListAsync();
            return Ok(addons);
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _context.PosSettings.ToListAsync();
            
            var taxRateStr = settings.FirstOrDefault(s => s.Key == "tax_rate")?.Value ?? "0";
            var footer = settings.FirstOrDefault(s => s.Key == "receipt_footer")?.Value ?? "";

            decimal.TryParse(taxRateStr, out decimal taxRate);

            return Ok(new PosSettingsDto { TaxRate = taxRate, ReceiptFooter = footer });
        }

        [HttpPost("validate-promo")]
        public async Task<IActionResult> ValidatePromo([FromBody] ValidatePromoRequest req)
        {
            var promo = await _context.Promos.FirstOrDefaultAsync(p => p.Code.ToLower() == req.PromoCode.ToLower());
            if (promo == null || !promo.IsActive || (promo.ValidUntil.HasValue && promo.ValidUntil < DateTime.UtcNow))
                return BadRequest(new { error = "Invalid or expired promo code" });

            return Ok(new { Code = promo.Code, DiscountType = promo.DiscountType, Value = promo.Value });
        }


        // ==========================================
        // 2. CHECKOUT & HISTORY
        // ==========================================

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest req)
        {
            if (req.Items == null || !req.Items.Any())
                return BadRequest(new { error = "Order must contain at least one item." });

            var cashierName = User.FindFirstValue("displayName") ?? "Unknown";
            
            // Get Settings for tax and footer
            var settings = await _context.PosSettings.ToListAsync();
            var taxRateStr = settings.FirstOrDefault(s => s.Key == "tax_rate")?.Value ?? "0";
            var receiptFooter = settings.FirstOrDefault(s => s.Key == "receipt_footer")?.Value ?? "";
            decimal.TryParse(taxRateStr, out decimal taxRatePercent);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();
                
                foreach (var itemReq in req.Items)
                {
                    var product = await _context.Products
                        .Include(p => p.Ingredients)
                        .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId);
                    if (product == null || !product.IsAvailable)
                        return BadRequest(new { error = $"Product ID {itemReq.ProductId} is unavailable." });

                    decimal sizePrice = 0;
                    if (!string.IsNullOrEmpty(itemReq.Size))
                    {
                        var sizeMod = await _context.SizeModifiers.FirstOrDefaultAsync(s => s.SizeName == itemReq.Size);
                        if (sizeMod != null) sizePrice = sizeMod.PriceModifier;
                    }

                    decimal addonsTotal = 0;
                    string customizationsJson = null;
                    if (itemReq.AddOns != null && itemReq.AddOns.Any())
                    {
                        addonsTotal = itemReq.AddOns.Sum(a => a.Price);
                        customizationsJson = JsonSerializer.Serialize(itemReq.AddOns);
                    }

                    var itemUnitSubtotal = product.Price + sizePrice + addonsTotal;
                    var itemSubtotal = itemUnitSubtotal * itemReq.Quantity;
                    subtotal += itemSubtotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = itemReq.Quantity,
                        UnitPrice = product.Price,
                        Size = itemReq.Size,
                        SizeModifierPrice = sizePrice,
                        Temperature = itemReq.Temperature,
                        IceLevel = itemReq.IceLevel,
                        SugarLevel = itemReq.SugarLevel,
                        CustomizationsJson = customizationsJson,
                        AddonsTotal = addonsTotal,
                        Subtotal = itemSubtotal
                    });

                    // Inventory deduction moved to IInventoryManager
                }

                // Handle Discount
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(req.DiscountType) && req.DiscountValue > 0)
                {
                    if (req.DiscountType == "percentage")
                        discountAmount = subtotal * (req.DiscountValue / 100);
                    else if (req.DiscountType == "fixed")
                        discountAmount = req.DiscountValue;
                }
                
                // If promo code was applied on top or instead
                if (!string.IsNullOrEmpty(req.PromoCode))
                {
                    var promo = await _context.Promos.FirstOrDefaultAsync(p => p.Code.ToLower() == req.PromoCode.ToLower());
                    if (promo != null && promo.IsActive)
                    {
                        if (promo.DiscountType == "percentage")
                            discountAmount = subtotal * (promo.Value / 100);
                        else if (promo.DiscountType == "fixed")
                            discountAmount = promo.Value;
                    }
                }

                if (discountAmount > subtotal) discountAmount = subtotal;

                var discountedSubtotal = subtotal - discountAmount;
                var taxAmount = discountedSubtotal * (taxRatePercent / 100);
                var totalAmount = discountedSubtotal + taxAmount;

                var change = req.PaymentMode == "Cash" ? Math.Max(0, req.AmountTendered - totalAmount) : 0;

                // Generate Order Number
                var today = DateTime.UtcNow;
                var todayStr = today.ToString("yyyyMMdd");
                var countToday = await _context.Orders.CountAsync(o => o.OrderNumber.StartsWith($"ORD-{todayStr}"));
                var orderNumber = $"ORD-{todayStr}-{(countToday + 1):D4}";

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = today,
                    OrderType = req.OrderType,
                    Subtotal = subtotal,
                    DiscountType = req.DiscountType ?? (req.PromoCode != null ? "promo" : null),
                    DiscountValue = req.DiscountValue,
                    PromoCode = req.PromoCode,
                    PromoDiscount = discountAmount,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    PaymentMode = req.PaymentMode,
                    AmountTendered = req.PaymentMode == "Cash" ? req.AmountTendered : totalAmount,
                    ChangeAmount = change,
                    CashierName = cashierName,
                    ReceiptFooter = receiptFooter,
                    Items = orderItems
                };

                order.PaymentStatus = req.PaymentMode == "E-Wallet" ? "Pending" : "Completed";

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                int userId = 1;
                if (int.TryParse(User.FindFirstValue("id"), out int parsedUserId))
                    userId = parsedUserId;

                await _inventoryManager.DeductStockForOrderAsync(order, userId);
                await _cafeContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (req.PaymentMode == "E-Wallet")
                {
                    if (!req.UseXendit)
                    {
                        order.PaymentReference = req.ReferenceCode;
                        order.PaymentStatus = "Completed";
                        await _context.SaveChangesAsync();
                        return StatusCode(201, new { message = "Order created successfully with manual reference.", orderId = order.Id, orderNumber = order.OrderNumber });
                    }

                    var apiKey = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(HttpContext.RequestServices)["XenditApiKey"];
                    if (string.IsNullOrEmpty(apiKey)) throw new Exception("Xendit API key not configured.");
                    
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiKey}:")));

                    var payload = new
                    {
                        external_id = order.OrderNumber,
                        amount = (long)order.TotalAmount,
                        payer_email = "customer@cafemarahuyo.com",
                        description = $"Cafe Marahuyo Order {order.OrderNumber}"
                    };

                    try
                    {
                        var content = new System.Net.Http.StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync("https://api.xendit.co/v2/invoices", content);
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                            throw new Exception($"Xendit API returned {response.StatusCode}: {responseString}");

                        var responseData = JsonSerializer.Deserialize<JsonElement>(responseString);
                        var invoiceId = responseData.GetProperty("id").GetString();
                        var invoiceUrl = responseData.GetProperty("invoice_url").GetString();

                        order.PaymentReference = invoiceId;
                        await _context.SaveChangesAsync();

                        return StatusCode(201, new 
                        { 
                            message = "Order created successfully. Please pay using the link.", 
                            orderId = order.Id, 
                            orderNumber = order.OrderNumber,
                            paymentUrl = invoiceUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { error = "Failed to generate E-Wallet payment link: " + ex.Message });
                    }
                }

                return StatusCode(201, new { message = "Order created successfully", orderId = order.Id, orderNumber = order.OrderNumber });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Server error during checkout" });
            }
        }

        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
                
            if (order == null) return NotFound();

            var dto = MapToOrderDto(order);
            return Ok(dto);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string? search, [FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string? paymentMode, [FromQuery] string? orderType)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(s) || 
                                         o.CashierName.ToLower().Contains(s) ||
                                         o.Items.Any(i => i.Product.Name.ToLower().Contains(s)));
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime from))
            {
                query = query.Where(o => o.OrderDate >= from.ToUniversalTime());
            }
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime to))
            {
                query = query.Where(o => o.OrderDate <= to.ToUniversalTime().AddDays(1).AddTicks(-1));
            }
            if (!string.IsNullOrEmpty(paymentMode))
            {
                query = query.Where(o => o.PaymentMode == paymentMode);
            }
            if (!string.IsNullOrEmpty(orderType))
            {
                query = query.Where(o => o.OrderType == orderType);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).Take(100).ToListAsync();
            var dtos = orders.Select(MapToOrderDto);
            return Ok(dtos);
        }

        [HttpGet("history/summary")]
        public async Task<IActionResult> GetHistorySummary([FromQuery] string period = "daily") // daily, weekly, monthly
        {
            var query = _context.Orders.AsQueryable();
            var now = DateTime.UtcNow;

            if (period == "daily")
                query = query.Where(o => o.OrderDate >= now.Date);
            else if (period == "weekly")
                query = query.Where(o => o.OrderDate >= now.Date.AddDays(-7));
            else if (period == "monthly")
                query = query.Where(o => o.OrderDate >= now.Date.AddDays(-30));

            var totalRevenue = await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var totalOrders = await query.CountAsync();

            return Ok(new SalesSummaryDto
            {
                Period = period,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders
            });
        }

        private OrderDto MapToOrderDto(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                OrderType = o.OrderType,
                Subtotal = o.Subtotal,
                TaxAmount = o.TaxAmount,
                TotalAmount = o.TotalAmount,
                PaymentMode = o.PaymentMode,
                DiscountType = o.DiscountType,
                DiscountValue = o.DiscountValue,
                PromoCode = o.PromoCode,
                PromoDiscount = o.PromoDiscount,
                AmountTendered = o.AmountTendered,
                ChangeAmount = o.ChangeAmount,
                CashierName = o.CashierName,
                ReceiptFooter = o.ReceiptFooter,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Unknown",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Size = i.Size,
                    SizeModifierPrice = i.SizeModifierPrice,
                    Temperature = i.Temperature,
                    IceLevel = i.IceLevel,
                    SugarLevel = i.SugarLevel,
                    CustomizationsJson = i.CustomizationsJson,
                    AddonsTotal = i.AddonsTotal,
                    Subtotal = i.Subtotal
                }).ToList()
            };
        }

        // ==========================================
        // 3. SUPERADMIN PANEL
        // ==========================================

        [HttpGet("products/all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllProductsAdmin()
        {
            var products = await _context.Products.OrderBy(p => p.CategoryName).ThenBy(p => p.Name).ToListAsync();
            return Ok(products.Select(p => new ProductDto { Id = p.Id, Name = p.Name, CategoryName = p.CategoryName, Price = p.Price, Description = p.Description, IsAvailable = p.IsAvailable }));
        }

        [HttpPost("products")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddProduct([FromBody] CreateProductRequest req)
        {
            var product = new Product
            {
                Name = req.Name,
                CategoryName = req.CategoryName,
                Price = req.Price,
                ImageUrl = req.ImageUrl,
                Description = req.Description,
                IsAvailable = true
            };
            _context.Products.Add(product);
            await LogAudit("Add", product.Name, $"Added to {product.CategoryName} at ₱{product.Price:F2}");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added successfully", id = product.Id });
        }

        [HttpPut("products/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest req)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.Name = req.Name;
            p.CategoryName = req.CategoryName;
            p.Price = req.Price;
            p.IsAvailable = req.IsAvailable;
            p.Description = req.Description;
            await LogAudit("Edit", p.Name, "Details updated");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product updated" });
        }

        [HttpDelete("products/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.IsAvailable = false;
            await LogAudit("Remove", p.Name, "Soft deleted from menu");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product removed" });
        }

        [HttpGet("size-modifiers/all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllSizeModifiers()
        {
            return Ok(await _context.SizeModifiers.OrderBy(s => s.SortOrder).ToListAsync());
        }

        [HttpPut("size-modifiers/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateSizeModifier(int id, [FromBody] UpdateSizeModifierRequest req)
        {
            var s = await _context.SizeModifiers.FindAsync(id);
            if (s == null) return NotFound();
            s.PriceModifier = req.PriceModifier;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size price updated" });
        }

        [HttpPatch("size-modifiers/{id}/toggle")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ToggleSizeModifier(int id)
        {
            var s = await _context.SizeModifiers.FindAsync(id);
            if (s == null) return NotFound();
            s.IsActive = !s.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size visibility toggled" });
        }

        [HttpGet("addons/all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllAddOns()
        {
            return Ok(await _context.AddOns.OrderBy(a => a.Category).ThenBy(a => a.SortOrder).ToListAsync());
        }

        [HttpPost("addons")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateAddOn([FromBody] CreateAddOnRequest req)
        {
            var maxSort = await _context.AddOns.Where(a => a.Category == req.Category).MaxAsync(a => (int?)a.SortOrder) ?? 0;
            var a = new AddOn { Category = req.Category, Name = req.Name, Price = req.Price, SortOrder = maxSort + 1 };
            _context.AddOns.Add(a);
            await _context.SaveChangesAsync();
            return Ok(a);
        }

        [HttpPut("addons/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateAddOn(int id, [FromBody] UpdateAddOnRequest req)
        {
            var a = await _context.AddOns.FindAsync(id);
            if (a == null) return NotFound();
            a.Category = req.Category;
            a.Name = req.Name;
            a.Price = req.Price;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Add-on updated" });
        }

        [HttpPatch("addons/{id}/toggle")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ToggleAddOn(int id)
        {
            var a = await _context.AddOns.FindAsync(id);
            if (a == null) return NotFound();
            a.IsActive = !a.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Add-on toggled" });
        }

        [HttpDelete("addons/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteAddOn(int id)
        {
            var a = await _context.AddOns.FindAsync(id);
            if (a == null) return NotFound();
            _context.AddOns.Remove(a);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Add-on deleted" });
        }

        [HttpPut("settings")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdatePosSettingsRequest req)
        {
            var taxSetting = await _context.PosSettings.FirstOrDefaultAsync(s => s.Key == "tax_rate");
            if (taxSetting == null) { taxSetting = new PosSettings { Key = "tax_rate" }; _context.PosSettings.Add(taxSetting); }
            taxSetting.Value = req.TaxRate.ToString();

            var footerSetting = await _context.PosSettings.FirstOrDefaultAsync(s => s.Key == "receipt_footer");
            if (footerSetting == null) { footerSetting = new PosSettings { Key = "receipt_footer" }; _context.PosSettings.Add(footerSetting); }
            footerSetting.Value = req.ReceiptFooter;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Settings updated" });
        }

        [HttpGet("products/archives")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetProductArchives()
        {
            var logs = await _context.ProductArchives.OrderByDescending(l => l.Timestamp).Take(100).ToListAsync();
            return Ok(logs.Select(l => new ProductAuditLogDto
            {
                Id = l.Id,
                Action = l.Action,
                ProductName = l.ProductName,
                PerformedBy = l.PerformedBy,
                Timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                Details = l.Details
            }));
        }

        private async Task LogAudit(string action, string productName, string details)
        {
            var cashierName = User.FindFirstValue("displayName") ?? "Unknown";
            _context.ProductArchives.Add(new ProductArchive
            {
                Action = action,
                ProductName = productName,
                PerformedBy = cashierName,
                Details = details
            });
        }

        // ==========================================
        // 4. WEBHOOKS
        // ==========================================

        [HttpPost("xendit-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> XenditWebhook([FromBody] System.Text.Json.JsonElement payload)
        {
            try
            {
                // Basic validation (In production, validate Xendit callback token)
                var externalId = payload.GetProperty("external_id").GetString();
                var status = payload.GetProperty("status").GetString();

                if (string.IsNullOrEmpty(externalId)) return BadRequest();

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == externalId);
                if (order != null)
                {
                    if (status == "PAID" || status == "SETTLED")
                    {
                        order.PaymentStatus = "Completed";
                        await _context.SaveChangesAsync();
                    }
                    else if (status == "EXPIRED")
                    {
                        order.PaymentStatus = "Expired";
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Webhook processing failed", details = ex.Message });
            }
        }
    }
}
