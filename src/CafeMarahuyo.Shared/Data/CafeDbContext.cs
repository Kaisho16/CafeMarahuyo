using CafeMarahuyo.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMarahuyo.Shared.Data
{
    public class CafeDbContext : DbContext
    {
        public CafeDbContext(DbContextOptions<CafeDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<InventoryBatch> InventoryBatches { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            
            modelBuilder.Entity<InventoryItem>().HasIndex(i => i.CategoryId);
            modelBuilder.Entity<InventoryItem>().HasIndex(i => i.Quantity);
            
            modelBuilder.Entity<InventoryBatch>().HasIndex(b => b.InventoryItemId);
            modelBuilder.Entity<InventoryBatch>().HasIndex(b => b.ExpirationDate);
            
            modelBuilder.Entity<Transaction>().HasIndex(t => t.ItemId);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.CreatedAt);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.Type);

            // Relationships
            modelBuilder.Entity<InventoryItem>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryBatch>()
                .HasOne(b => b.Item)
                .WithMany(i => i.Batches)
                .HasForeignKey(b => b.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Item)
                .WithMany()
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<System.DateTime, System.DateTime>(
                v => v.ToUniversalTime(),
                v => System.DateTime.SpecifyKind(v, System.DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<System.DateTime?, System.DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? System.DateTime.SpecifyKind(v.Value, System.DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(System.DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(System.DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}
