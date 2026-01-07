namespace NivaTradeDocs.Models
{
    public class SpecificationItem
    {
        public int Id { get; set; }
        public string SpecificationId { get; set; }
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
    }
}
