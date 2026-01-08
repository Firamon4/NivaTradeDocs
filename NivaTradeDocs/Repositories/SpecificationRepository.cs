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
                    IsApproved = dto.IsApproved,
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
                existing.IsApproved = dto.IsApproved;

                // Видаляємо старі рядки і пишемо нові
                _context.SpecificationItems.RemoveRange(existing.Items);

                if (dto.Items != null && !dto.IsDeleted && dto.IsApproved)
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

        public async Task<List<SpecItemDisplayModel>> GetLatestItemsByCounterpartyAsync(string counterpartyId)
        {
            // 1. Шукаємо останню специфікацію (сортуємо за Датою спаданням)
            var latestSpec = await _context.Specifications
                                           .Where(s => s.CounterpartyId == counterpartyId && !s.IsDeleted && s.IsApproved)
                                           .OrderByDescending(s => s.Date)
                                           .FirstOrDefaultAsync();

            if (latestSpec == null) return new List<SpecItemDisplayModel>();

            // 2. Беремо її рядки
            var specItems = await _context.SpecificationItems
                                          .Where(si => si.SpecificationId == latestSpec.Id)
                                          .ToListAsync();

            // 3. З'єднуємо з товарами, щоб отримати Назви
            var productIds = specItems.Select(si => si.ProductId).ToList();
            var products = await _context.Products
                                         .Where(p => productIds.Contains(p.Id))
                                         .ToDictionaryAsync(p => p.Id, p => p.Name);

            // 4. Формуємо красивий список для екрану
            var result = new List<SpecItemDisplayModel>();
            foreach (var item in specItems)
            {
                if (products.TryGetValue(item.ProductId, out var prodName))
                {
                    result.Add(new SpecItemDisplayModel
                    {
                        ProductId = item.ProductId,
                        ProductName = prodName,
                        Price = item.Price,
                        Unit = item.Unit ?? "шт"
                    });
                }
            }

            return result.OrderBy(x => x.ProductName).ToList();
        }
    }
}