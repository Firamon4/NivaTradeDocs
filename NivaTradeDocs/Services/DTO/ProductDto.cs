namespace NivaTradeDocs.Services.DTO
{
    public class ProductDto
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string Articul { get; set; }
        public bool IsFolder { get; set; }
        public bool IsDeleted { get; set; }
        public string ParentUid { get; set; }
    }
}
