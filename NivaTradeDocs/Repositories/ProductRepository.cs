using NivaTradeDocs.Data;
using NivaTradeDocs.Models; 
using NivaTradeDocs.Services.DTO;


namespace NivaTradeDocs.Repositories
{
    public class ProductRepository
    {
        private readonly PosDbContext _context;

        public ProductRepository(PosDbContext context)
        {
            _context = context;
        }

        public async Task UpsertAsync(ProductDto dto)
        {
            var existing = await _context.Products.FindAsync(dto.Uid);

            if (existing == null)
            {
                _context.Products.Add(new Product
                {
                    Id = dto.Uid,
                    Name = dto.Name,
                    Code = dto.Code,
                    Barcode = dto.Barcode,
                    Articul = dto.Articul,
                    IsFolder = dto.IsFolder,
                    ParentId = string.IsNullOrEmpty(dto.ParentUid) ? null : dto.ParentUid
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code;
                existing.Barcode = dto.Barcode;
                existing.Articul = dto.Articul;
                existing.IsFolder = dto.IsFolder;
                existing.ParentId = string.IsNullOrEmpty(dto.ParentUid) ? null : dto.ParentUid;

                if (dto.IsDeleted) _context.Products.Remove(existing);
            }
        }
    }
}