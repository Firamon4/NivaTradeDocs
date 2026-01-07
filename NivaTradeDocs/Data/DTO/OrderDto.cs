namespace NivaTradeDocs.Data.DTO
{
    public class OrderDto
    {
        public string Uid { get; set; }
        public DateTime Date { get; set; }
        public string CounterpartyUid { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }
}
