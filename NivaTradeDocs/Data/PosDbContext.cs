using Microsoft.EntityFrameworkCore;

namespace NivaTradeDocs.Data
{
    // Опис нашої локальної бази
    public class PosDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Counterparty> Counterparties { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // База створиться прямо в папці з програмою (файл niva_pos.db)
            optionsBuilder.UseSqlite("Data Source=niva_pos.db");
        }
    }

    public class Product
    {
        public string  Id       { get; set; }
        public string  Name     { get; set; }

        public string? Code     { get; set; }   // Може бути пустим
        public string? Barcode  { get; set; }   // Може бути пустим
        public string? Articul  { get; set; }   // Може бути пустим

        public bool    IsFolder { get; set; }
        public string? ParentId { get; set; }
    }

    public class Counterparty
    {
        public string  Id { get; set; } 
        public string  Name { get; set; }
        public string? Code { get; set; }
        public string? TaxId { get; set; } 
        public bool    IsDeleted { get; set; }
    }

    public class Specification
    {
        public string Id { get; set; } 
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string CounterpartyId { get; set; } 
        public bool IsDeleted { get; set; }
        public List<SpecificationItem> Items { get; set; } = new List<SpecificationItem>();
    }

    public class SpecificationItem
    {
        public int Id { get; set; }
        public string SpecificationId { get; set; }
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
    }

    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); 
        public DateTime Date { get; set; } = DateTime.Now;
        public string CounterpartyId { get; set; }

        public bool IsSent { get; set; } = false; 

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal Price { get; set; } 
        public decimal Sum => Quantity * Price; 
    }
}