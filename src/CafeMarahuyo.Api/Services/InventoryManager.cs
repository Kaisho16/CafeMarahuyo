using System;
using System.Linq;
using System.Threading.Tasks;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMarahuyo.Api.Services
{
    public interface IInventoryManager
    {
        Task DeductStockForOrderAsync(Order order, int performedByUserId);
    }

    public class InventoryManager : IInventoryManager
    {
        private readonly CafeDbContext _context;

        public InventoryManager(CafeDbContext context)
        {
            _context = context;
        }

        public async Task DeductStockForOrderAsync(Order order, int performedByUserId)
        {
            foreach (var item in order.Items)
            {
                if (item.Product == null || item.Product.Ingredients == null) continue;

                foreach (var ingredient in item.Product.Ingredients)
                {
                    decimal quantityToDeduct = ingredient.QuantityRequired * item.Quantity;

                    var invItem = await _context.InventoryItems
                        .Include(i => i.Batches)
                        .FirstOrDefaultAsync(i => i.Id == ingredient.InventoryItemId);

                    if (invItem != null && invItem.Quantity >= quantityToDeduct)
                    {
                        var previousQty = invItem.Quantity;
                        invItem.Quantity -= quantityToDeduct;
                        invItem.UpdatedAt = DateTime.UtcNow;

                        decimal remaining = quantityToDeduct;
                        var orderedBatches = invItem.Batches
                            .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)
                            .ThenBy(b => b.ExpirationDate)
                            .ToList();

                        foreach (var batch in orderedBatches)
                        {
                            if (remaining <= 0) break;
                            if (batch.Quantity > 0)
                            {
                                decimal deductAmt = Math.Min(batch.Quantity, remaining);
                                batch.Quantity -= deductAmt;
                                batch.UpdatedAt = DateTime.UtcNow;
                                remaining -= deductAmt;
                            }
                        }

                        _context.Transactions.Add(new Transaction
                        {
                            ItemId = invItem.Id,
                            Type = "stock_out",
                            Quantity = quantityToDeduct,
                            PreviousQuantity = previousQty,
                            NewQuantity = invItem.Quantity,
                            Notes = $"Auto-deducted for Order {order.OrderNumber}",
                            PerformedBy = performedByUserId,
                            CreatedAt = DateTime.UtcNow
                        });

                        var emptyBatches = invItem.Batches.Where(b => b.Quantity == 0).ToList();
                        _context.InventoryBatches.RemoveRange(emptyBatches);
                    }
                }
            }
        }
    }
}
