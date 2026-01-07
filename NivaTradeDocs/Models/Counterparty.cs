namespace NivaTradeDocs.Models
{
    public class Counterparty
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string? TaxId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
