namespace NivaTradeDocs.Models
{
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
