using ProvaPub.Models;

namespace ProvaPub.Repository
{
    public interface IRandomNumberRepository
    {
        Task<bool> ExistsAsync(int number, CancellationToken ct = default);
        Task AddAsync(RandomNumber number, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
