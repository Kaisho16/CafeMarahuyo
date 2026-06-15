using System;
using System.Linq;
using CafeMarahuyo.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMarahuyo.Shared.Data
{
    public static class DbInitializer
    {
        public static void Initialize(CafeDbContext context)
        {
            context.Database.Migrate();

            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Users
            var users = new User[]
            {
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("marahuyo2024"), DisplayName = "Admin", Role = "admin" },
                new User { Username = "staff", PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff2024"), DisplayName = "Staff Member", Role = "staff" }
            };
            foreach (var u in users)
            {
                context.Users.Add(u);
            }
            context.SaveChanges();

            // Categories
            var categories = new Category[]
            {
                new Category { Name = "Coffee Beans", Description = "Various coffee bean varieties and blends", Icon = "coffee" },
                new Category { Name = "Milk & Dairy", Description = "Fresh milk, cream, and dairy alternatives", Icon = "water_drop" },
                new Category { Name = "Syrups & Flavoring", Description = "Flavored syrups, sauces, and extracts", Icon = "local_bar" },
                new Category { Name = "Pastries & Food", Description = "Baked goods, sandwiches, and food items", Icon = "bakery_dining" },
                new Category { Name = "Supplies", Description = "Cups, lids, straws, napkins, and other supplies", Icon = "inventory_2" }
            };
            foreach (var c in categories)
            {
                context.Categories.Add(c);
            }
            context.SaveChanges();

            var catLookup = context.Categories.ToDictionary(c => c.Name, c => c.Id);

            // Inventory Items (No expiration dates on the item itself anymore)
            var items = new InventoryItem[]
            {
                new InventoryItem { Name = "Arabica Beans (House Blend)", CategoryId = catLookup["Coffee Beans"], Quantity = 25, Unit = "kg", CostPerUnit = 850.00m, MinStockLevel = 5, Description = "Premium house blend arabica beans" },
                new InventoryItem { Name = "Robusta Beans", CategoryId = catLookup["Coffee Beans"], Quantity = 18, Unit = "kg", CostPerUnit = 620.00m, MinStockLevel = 5, Description = "Strong robusta for espresso blends" },
                new InventoryItem { Name = "Decaf Beans", CategoryId = catLookup["Coffee Beans"], Quantity = 8, Unit = "kg", CostPerUnit = 950.00m, MinStockLevel = 3, Description = "Swiss water process decaf beans" },
                new InventoryItem { Name = "Matcha Powder", CategoryId = catLookup["Coffee Beans"], Quantity = 3, Unit = "kg", CostPerUnit = 2200.00m, MinStockLevel = 2, Description = "Ceremonial grade matcha from Japan" },

                new InventoryItem { Name = "Fresh Whole Milk", CategoryId = catLookup["Milk & Dairy"], Quantity = 45, Unit = "liters", CostPerUnit = 85.00m, MinStockLevel = 15, Description = "Full cream fresh milk" },
                new InventoryItem { Name = "Oat Milk", CategoryId = catLookup["Milk & Dairy"], Quantity = 20, Unit = "liters", CostPerUnit = 180.00m, MinStockLevel = 8, Description = "Barista edition oat milk" },
                new InventoryItem { Name = "Heavy Cream", CategoryId = catLookup["Milk & Dairy"], Quantity = 12, Unit = "liters", CostPerUnit = 220.00m, MinStockLevel = 5, Description = "For whipped cream and specialty drinks" },
                new InventoryItem { Name = "Almond Milk", CategoryId = catLookup["Milk & Dairy"], Quantity = 10, Unit = "liters", CostPerUnit = 195.00m, MinStockLevel = 5, Description = "Unsweetened almond milk" },

                new InventoryItem { Name = "Vanilla Syrup", CategoryId = catLookup["Syrups & Flavoring"], Quantity = 15, Unit = "bottles", CostPerUnit = 320.00m, MinStockLevel = 4, Description = "750ml bottles of vanilla syrup" },
                new InventoryItem { Name = "Caramel Syrup", CategoryId = catLookup["Syrups & Flavoring"], Quantity = 12, Unit = "bottles", CostPerUnit = 320.00m, MinStockLevel = 4, Description = "750ml bottles of caramel syrup" },
                new InventoryItem { Name = "Hazelnut Syrup", CategoryId = catLookup["Syrups & Flavoring"], Quantity = 8, Unit = "bottles", CostPerUnit = 340.00m, MinStockLevel = 3, Description = "750ml bottles of hazelnut syrup" },
                new InventoryItem { Name = "Chocolate Sauce", CategoryId = catLookup["Syrups & Flavoring"], Quantity = 6, Unit = "bottles", CostPerUnit = 280.00m, MinStockLevel = 3, Description = "Rich chocolate sauce for mochas" },

                new InventoryItem { Name = "Croissants", CategoryId = catLookup["Pastries & Food"], Quantity = 24, Unit = "pcs", CostPerUnit = 45.00m, MinStockLevel = 10, Description = "Butter croissants, baked fresh daily" },
                new InventoryItem { Name = "Banana Bread", CategoryId = catLookup["Pastries & Food"], Quantity = 12, Unit = "pcs", CostPerUnit = 65.00m, MinStockLevel = 5, Description = "Homemade banana bread slices" },
                new InventoryItem { Name = "Cookies (Assorted)", CategoryId = catLookup["Pastries & Food"], Quantity = 36, Unit = "pcs", CostPerUnit = 35.00m, MinStockLevel = 15, Description = "Chocolate chip, oatmeal, and snickerdoodle" },
                new InventoryItem { Name = "Ham & Cheese Sandwich", CategoryId = catLookup["Pastries & Food"], Quantity = 8, Unit = "pcs", CostPerUnit = 85.00m, MinStockLevel = 5, Description = "Classic ham and cheese panini" },

                new InventoryItem { Name = "Paper Cups (12oz)", CategoryId = catLookup["Supplies"], Quantity = 450, Unit = "pcs", CostPerUnit = 5.50m, MinStockLevel = 100, Description = "Branded paper cups" },
                new InventoryItem { Name = "Paper Cups (16oz)", CategoryId = catLookup["Supplies"], Quantity = 380, Unit = "pcs", CostPerUnit = 6.50m, MinStockLevel = 100, Description = "Large branded paper cups" },
                new InventoryItem { Name = "Plastic Lids", CategoryId = catLookup["Supplies"], Quantity = 500, Unit = "pcs", CostPerUnit = 2.50m, MinStockLevel = 150, Description = "Dome lids for iced drinks" },
                new InventoryItem { Name = "Paper Straws", CategoryId = catLookup["Supplies"], Quantity = 600, Unit = "pcs", CostPerUnit = 3.00m, MinStockLevel = 200, Description = "Eco-friendly paper straws" },
                new InventoryItem { Name = "Napkins", CategoryId = catLookup["Supplies"], Quantity = 800, Unit = "pcs", CostPerUnit = 1.50m, MinStockLevel = 200, Description = "Branded napkins" },
                new InventoryItem { Name = "Sugar Packets", CategoryId = catLookup["Supplies"], Quantity = 350, Unit = "pcs", CostPerUnit = 2.00m, MinStockLevel = 100, Description = "Individual sugar packets" }
            };
            foreach (var i in items)
            {
                context.InventoryItems.Add(i);
            }
            context.SaveChanges();
            
            // Generate some batches for expiration dates
            var rand = new Random();
            foreach (var item in items)
            {
                if (item.Category?.Name == "Supplies")
                {
                    // Non-expiring batches
                    context.InventoryBatches.Add(new InventoryBatch { InventoryItemId = item.Id, Quantity = item.Quantity, ExpirationDate = null });
                }
                else
                {
                    // Split quantity into two batches to demonstrate overlapping dates
                    int qty1 = item.Quantity / 2;
                    int qty2 = item.Quantity - qty1;
                    
                    DateTime baseDate = DateTime.UtcNow.AddDays(rand.Next(10, 60));
                    
                    if (qty1 > 0)
                        context.InventoryBatches.Add(new InventoryBatch { InventoryItemId = item.Id, Quantity = qty1, ExpirationDate = baseDate });
                    if (qty2 > 0)
                        context.InventoryBatches.Add(new InventoryBatch { InventoryItemId = item.Id, Quantity = qty2, ExpirationDate = baseDate.AddDays(rand.Next(15, 30)) });
                }
            }
            context.SaveChanges();

            SeedSampleTransactions(context);
        }

        private static void SeedSampleTransactions(CafeDbContext context)
        {
            var items = context.InventoryItems.ToList();
            var batches = context.InventoryBatches.ToList();
            if (!items.Any()) return;

            var admin = context.Users.FirstOrDefault(u => u.Role == "admin");
            if (admin == null) return;

            var rand = new Random();

            for (int dayOffset = 6; dayOffset >= 0; dayOffset--)
            {
                var date = DateTime.UtcNow.AddDays(-dayOffset);

                int txCount = 2 + rand.Next(3);
                for (int t = 0; t < txCount; t++)
                {
                    var item = items[rand.Next(items.Count)];
                    var batch = batches.FirstOrDefault(b => b.InventoryItemId == item.Id);
                    
                    bool isStockIn = rand.NextDouble() > 0.6;
                    int qty = 1 + rand.Next(10);
                    int prevQty = item.Quantity;
                    int newQty = isStockIn ? prevQty + qty : Math.Max(0, prevQty - qty);

                    var txDate = date.AddHours(-rand.Next(8)).AddMinutes(-rand.Next(60));

                    context.Transactions.Add(new Transaction
                    {
                        ItemId = item.Id,
                        BatchId = batch?.Id,
                        Type = isStockIn ? "stock_in" : "stock_out",
                        Quantity = qty,
                        PreviousQuantity = prevQty,
                        NewQuantity = newQty,
                        Notes = isStockIn ? "Regular restocking" : "Daily usage",
                        PerformedBy = admin.Id,
                        CreatedAt = txDate
                    });
                }
            }
            context.SaveChanges();

        }
    }
}
