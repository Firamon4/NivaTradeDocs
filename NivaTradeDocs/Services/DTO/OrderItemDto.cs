using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NivaTradeDocs.Services.DTO
{
    public class OrderItemDto
    {
        public string ProductUid { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
