using System.Threading;
using System.Threading.Tasks;

namespace ProvaPub.Services.Payments
{
    public class PixPaymentProcessor : IPaymentProcessor
    {
        public string Method => "pix";

        public Task<(string TransactionId, string Provider, string Status)> PayAsync(
            decimal amount, int customerId, CancellationToken ct)
        {
            return Task.FromResult((Guid.NewGuid().ToString("N"), "Pix", "APPROVED"));
        }
    }
}