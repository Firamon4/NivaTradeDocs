namespace NivaTradeDocs.Services.DTO
{
    public class SpecificationDto
    {
        public string Uid { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string CounterpartyUid { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsApproved { get; set; }
        public List<SpecificationItemDto> Items { get; set; }
    }
}
