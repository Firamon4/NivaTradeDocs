namespace NivaTradeDocs.Models
{
    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string? Barcode { get; set; }
        public string? Articul { get; set; }
        public bool IsFolder { get; set; }
        public string? ParentId { get; set; }
    }
}
