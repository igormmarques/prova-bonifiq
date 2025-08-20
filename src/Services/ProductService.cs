using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;

namespace ProvaPub.Services
{
    public class ProductService
    {
        private readonly TestDbContext _ctx;

        public ProductService(TestDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<PagedResult<Product>> ListProductsAsync(int page, int pageSize = 10, CancellationToken ct = default)
        {
            page = Math.Max(1, page); // evita zero/negativo
            pageSize = Math.Max(1, pageSize);

            var query = _ctx.Products
                            .AsNoTracking()
                            .OrderBy(p => p.Id);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Product>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }
    }
}