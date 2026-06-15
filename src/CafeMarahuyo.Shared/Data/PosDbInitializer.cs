using System;
using System.Linq;
using CafeMarahuyo.Shared.Entities;

namespace CafeMarahuyo.Shared.Data
{
    public static class PosDbInitializer
    {
        public static void Initialize(PosDbContext context)
        {
            context.Database.EnsureCreated();

            // POS Settings Seed
            if (!context.PosSettings.Any())
            {
                context.PosSettings.Add(new PosSettings { Key = "tax_rate", Value = "0" });
                context.PosSettings.Add(new PosSettings { Key = "receipt_footer", Value = "Thank you for visiting Cafe Marahuyo! ☕" });
                context.SaveChanges();
            }

            // Size Modifiers Seed
            if (!context.SizeModifiers.Any())
            {
                var sizes = new SizeModifier[]
                {
                    new SizeModifier { SizeName = "Small", PriceModifier = 0m, SortOrder = 1 },
                    new SizeModifier { SizeName = "Medium", PriceModifier = 10m, SortOrder = 2 },
                    new SizeModifier { SizeName = "Large", PriceModifier = 20m, SortOrder = 3 }
                };
                context.SizeModifiers.AddRange(sizes);
                context.SaveChanges();
            }

            // Add-Ons Seed
            if (!context.AddOns.Any())
            {
                var addons = new AddOn[]
                {
                    // Milk Options
                    new AddOn { Category = "milk", Name = "Whole Milk", Price = 0m, SortOrder = 1 },
                    new AddOn { Category = "milk", Name = "Oat Milk", Price = 30m, SortOrder = 2 },
                    new AddOn { Category = "milk", Name = "Almond Milk", Price = 30m, SortOrder = 3 },
                    new AddOn { Category = "milk", Name = "Soy Milk", Price = 25m, SortOrder = 4 },
                    
                    // Espresso Shots
                    new AddOn { Category = "shot", Name = "+1 Espresso Shot", Price = 35m, SortOrder = 1 },
                    new AddOn { Category = "shot", Name = "+2 Espresso Shots", Price = 60m, SortOrder = 2 },

                    // Syrups
                    new AddOn { Category = "syrup", Name = "Vanilla", Price = 25m, SortOrder = 1 },
                    new AddOn { Category = "syrup", Name = "Caramel", Price = 25m, SortOrder = 2 },
                    new AddOn { Category = "syrup", Name = "Hazelnut", Price = 25m, SortOrder = 3 },
                    new AddOn { Category = "syrup", Name = "Brown Sugar", Price = 20m, SortOrder = 4 }
                };
                context.AddOns.AddRange(addons);
                context.SaveChanges();
            }

            // POS Products Seed
            if (!context.Products.Any())
            {
                var products = new Product[]
                {
                    new Product { Name = "Dark Mocha Frappe", CategoryName = "Frappe", Price = 190.00m, ImageUrl = "/pos/assets/dark-mocha.png", Description = "Rich dark chocolate with espresso and ice" },
                    new Product { Name = "Caramel Macchiato", CategoryName = "Coffee", Price = 180.00m, ImageUrl = "/pos/assets/caramel-macchiato.png", Description = "Espresso with vanilla and caramel drizzle" },
                    new Product { Name = "Matcha Latte", CategoryName = "Tea", Price = 195.00m, ImageUrl = "/pos/assets/matcha-latte.png", Description = "Premium Japanese matcha with steamed milk" },
                    new Product { Name = "Strawberry Soda", CategoryName = "Soda", Price = 150.00m, ImageUrl = "/pos/assets/strawberry-soda.png", Description = "Refreshing strawberry Italian soda" },
                    new Product { Name = "Americano", CategoryName = "Coffee", Price = 140.00m, ImageUrl = "/pos/assets/americano.png", Description = "Classic espresso pulled over hot water" },
                    new Product { Name = "Vanilla Bean Frappe", CategoryName = "Frappe", Price = 185.00m, ImageUrl = "/pos/assets/vanilla-frappe.png", Description = "Creamy vanilla blended beverage" },
                    new Product { Name = "Iced Lemon Tea", CategoryName = "Tea", Price = 130.00m, ImageUrl = "/pos/assets/lemon-tea.png", Description = "Fresh brewed tea with lemon" },
                    new Product { Name = "Mango Graham Shake", CategoryName = "Other Beverages", Price = 175.00m, ImageUrl = "/pos/assets/mango-graham.png", Description = "Fresh mangoes blended with graham crackers" }
                };
                context.Products.AddRange(products);
                context.SaveChanges();
            }

            // POS Promos Seed
            if (!context.Promos.Any())
            {
                var promos = new Promo[]
                {
                    new Promo { Code = "B1T1", DiscountType = "percentage", Value = 50.00m }, // 50% off (Buy 1 Take 1 equivalent)
                    new Promo { Code = "WELCOME10", DiscountType = "fixed", Value = 10.00m }
                };
                context.Promos.AddRange(promos);
                context.SaveChanges();
            }
        }
    }
}
