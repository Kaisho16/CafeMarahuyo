using System;
using Microsoft.EntityFrameworkCore;
using CafeMarahuyo.Shared.Entities;

namespace CafeMarahuyo.Shared.Data
{
    public class PosDbContext : DbContext
    {
        public PosDbContext(DbContextOptions<PosDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Promo> Promos { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<ProductAuditLog> ProductAuditLogs { get; set; } = null!;
        
        // New entities
        public DbSet<SizeModifier> SizeModifiers { get; set; } = null!;
        public DbSet<AddOn> AddOns { get; set; } = null!;
        public DbSet<PosSettings> PosSettings { get; set; } = null!;
        public DbSet<ProductIngredient> ProductIngredients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Date converter for SQLite (which doesn't support DateTime natively in some contexts)
            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            // Products
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name).IsUnique();

            // Product Ingredients
            modelBuilder.Entity<ProductIngredient>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Ingredients)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Promos
            modelBuilder.Entity<Promo>()
                .HasIndex(p => p.Code).IsUnique();

            // Orders
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber).IsUnique();
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            // Order Items
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // PosSettings
            modelBuilder.Entity<PosSettings>()
                .HasIndex(s => s.Key).IsUnique();
        }
    }
}
