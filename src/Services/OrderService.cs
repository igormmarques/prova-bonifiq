using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;
using ProvaPub.Services.Payments;

namespace ProvaPub.Services
{
    public class OrderService
    {
        private readonly TestDbContext _ctx;
        private readonly IDictionary<string, IPaymentProcessor> _processors;

        public OrderService(TestDbContext ctx, IEnumerable<IPaymentProcessor> processors)
        {
            _ctx = ctx;
            _processors = processors.ToDictionary(p => p.Method, p => p, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<Order> PayOrder(string paymentMethod, decimal paymentValue, int customerId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentNullException(nameof(paymentMethod));
            if (paymentValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(paymentValue));

            // Confere cliente
            var exists = await _ctx.Customers.AnyAsync(c => c.Id == customerId, ct);
            if (!exists)
                throw new InvalidOperationException($"Customer {customerId} not found.");

            if (!_processors.TryGetValue(paymentMethod, out var processor))
                throw new NotSupportedException($"Payment method '{paymentMethod}' is not supported.");

            var (transactionId, provider, status) = await processor.PayAsync(paymentValue, customerId, ct);

            var order = new Order
            {
                CustomerId = customerId,
                Value = paymentValue,
                PaymentMethod = paymentMethod,
                PaymentProvider = provider,
                PaymentStatus = status,
                PaymentTransactionId = transactionId,

                OrderDate = DateTime.UtcNow
            };

            _ctx.Orders.Add(order);
            await _ctx.SaveChangesAsync(ct);

            return order;
        }
    }
}