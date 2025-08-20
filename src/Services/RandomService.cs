using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;
using System.Security.Cryptography;

namespace ProvaPub.Services
{
	public class RandomService
    {
        private readonly TestDbContext _ctx;

        public RandomService(TestDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<int> GetRandomAsync(CancellationToken ct = default)
        {
            const int maxAttempts = 7;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var number = RandomNumberGenerator.GetInt32(0, 100);

                try
                {
                    _ctx.Numbers.Add(new RandomNumber { Number = number });
                    await _ctx.SaveChangesAsync(ct);

                    return number;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Se colidiu com número existente, tenta novamente
                    continue;
                }
            }

            throw new InvalidOperationException("Não foi possível gerar um número único após várias tentativas.");
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }
    }
}
