using NivaTradeDocs.Data;
using NivaTradeDocs.Models;
using NivaTradeDocs.Services.DTO;

namespace NivaTradeDocs.Repositories
{
    public class CounterpartyRepository
    {
        private readonly PosDbContext _context;

        public CounterpartyRepository(PosDbContext context)
        {
            _context = context;
        }

        public async Task UpsertAsync(CounterpartyDto dto)
        {
            var existing = await _context.Counterparties.FindAsync(dto.Uid);

            if (existing == null)
            {
                _context.Counterparties.Add(new Counterparty
                {
                    Id = dto.Uid,
                    Name = dto.Name,
                    Code = dto.Code,
                    TaxId = dto.TaxId,
                    IsDeleted = dto.IsDeleted
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code;
                existing.TaxId = dto.TaxId;
                existing.IsDeleted = dto.IsDeleted;
            }
        }
    }
}