using Microsoft.EntityFrameworkCore;
using NivaTradeDocs.Models;

namespace NivaTradeDocs.Data
{
    public class PosDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Counterparty> Counterparties { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public DbSet<SpecificationItem> SpecificationItems { get; set; }

        // Таблиці для Замовлень
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=niva_trade_v7.db");
        }
    }
}