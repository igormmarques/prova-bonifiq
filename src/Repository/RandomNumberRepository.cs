using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;

namespace ProvaPub.Repository
{
    public class RandomNumberRepository : IRandomNumberRepository
    {
        private readonly TestDbContext _ctx;
        public RandomNumberRepository(TestDbContext ctx)
        {
            _ctx = ctx;
        }

        public Task<bool> ExistsAsync(int number, CancellationToken ct = default)
            => _ctx.Numbers.AnyAsync(x => x.Number == number, ct);

        public Task AddAsync(RandomNumber number, CancellationToken ct = default)
            => _ctx.Numbers.AddAsync(number, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct = default)
            => _ctx.SaveChangesAsync(ct);
    }
}
