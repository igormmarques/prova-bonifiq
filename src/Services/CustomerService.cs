using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;
using ProvaPub.Services.Time;

namespace ProvaPub.Services
{
    public class CustomerService
    {
        private readonly TestDbContext _ctx;
        private readonly IDateTimeProvider _clock;

        public CustomerService(TestDbContext ctx, IDateTimeProvider clock)
        {
            _ctx = ctx;
            _clock = clock;
        }

        public async Task<PagedResult<Customer>> ListCustomersAsync(int page, int pageSize = 10, CancellationToken ct = default)
        {
            // Garantimos que ninguém passe página inválida
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            // OrderBy garante ordenação estável entre páginas
            var query = _ctx.Customers
                            .AsNoTracking()
                            .OrderBy(c => c.Id);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Customer>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<bool> CanPurchase(int customerId, decimal purchaseValue)
        {
            if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
            if (purchaseValue <= 0) throw new ArgumentOutOfRangeException(nameof(purchaseValue));

            var customerExists = await _ctx.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists)
                throw new InvalidOperationException($"Customer Id {customerId} does not exists");

            var nowUtc = _clock.UtcNow;

            // 1 por mês (janela de 1 mês pra trás a partir de agora)
            var baseDate = nowUtc.AddMonths(-1);
            var hasOrderInLastMonth = await _ctx.Orders
                .AsNoTracking()
                .AnyAsync(s => s.CustomerId == customerId && s.OrderDate >= baseDate);
            if (hasOrderInLastMonth)
                return false;

            // Primeira compra <= 100
            var hasAnyOrderEver = await _ctx.Orders
                .AsNoTracking()
                .AnyAsync(o => o.CustomerId == customerId);
            if (!hasAnyOrderEver && purchaseValue > 100m)
                return false;

            // Comercial e dia útil (UTC convertido “como está” — regra do desafio usa UTC direto)
            var isWeekend = nowUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var hour = nowUtc.Hour;
            var isBusinessHours = hour >= 8 && hour <= 18;

            if (isWeekend || !isBusinessHours)
                return false;

            return true;
        }
    }
}
