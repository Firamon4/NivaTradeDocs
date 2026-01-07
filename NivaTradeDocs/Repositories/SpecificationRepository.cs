using Microsoft.EntityFrameworkCore;
using NivaTradeDocs.Data;
using NivaTradeDocs.Models;
using NivaTradeDocs.Services.DTO;

namespace NivaTradeDocs.Repositories
{
    public class SpecificationRepository
    {
        private readonly PosDbContext _context;

        public SpecificationRepository(PosDbContext context)
        {
            _context = context;
        }

        public async Task UpsertAsync(SpecificationDto dto)
        {
            var existing = await _context.Specifications
                                   .Include(s => s.Items)
                                   .FirstOrDefaultAsync(s => s.Id == dto.Uid);

            if (existing == null)
            {
                var newSpec = new Specification
                {
                    Id = dto.Uid,
                    Number = dto.Number,
                    Date = dto.Date,
                    CounterpartyId = dto.CounterpartyUid,
                    IsDeleted = dto.IsDeleted,
                    Items = new List<SpecificationItem>()
                };

                if (dto.Items != null)
                {
                    foreach (var itemDto in dto.Items)
                    {
                        newSpec.Items.Add(new SpecificationItem
                        {
                            SpecificationId = dto.Uid,
                            ProductId = itemDto.ProductUid,
                            Price = itemDto.Price,
                            Unit = itemDto.Unit
                        });
                    }
                }
                _context.Specifications.Add(newSpec);
            }
            else
            {
                existing.Number = dto.Number;
                existing.Date = dto.Date;
                existing.CounterpartyId = dto.CounterpartyUid;
                existing.IsDeleted = dto.IsDeleted;

                // Видаляємо старі рядки і пишемо нові
                _context.SpecificationItems.RemoveRange(existing.Items);

                if (dto.Items != null && !dto.IsDeleted)
                {
                    foreach (var itemDto in dto.Items)
                    {
                        _context.SpecificationItems.Add(new SpecificationItem
                        {
                            SpecificationId = dto.Uid,
                            ProductId = itemDto.ProductUid,
                            Price = itemDto.Price,
                            Unit = itemDto.Unit
                        });
                    }
                }
            }
        }
    }
}