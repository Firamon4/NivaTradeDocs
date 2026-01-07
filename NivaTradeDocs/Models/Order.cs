namespace NivaTradeDocs.Models
{
    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Date { get; set; } = DateTime.Now;
        public string CounterpartyId { get; set; }
        public bool IsSent { get; set; } = false;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
