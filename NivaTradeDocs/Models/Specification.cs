namespace NivaTradeDocs.Models
{
    public class Specification
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string CounterpartyId { get; set; }
        public bool IsDeleted { get; set; }
        public List<SpecificationItem> Items { get; set; } = new List<SpecificationItem>();
    }
}
