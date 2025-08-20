using System.Threading;
using System.Threading.Tasks;

namespace ProvaPub.Services.Payments
{
    public class CreditCardPaymentProcessor : IPaymentProcessor
    {
        public string Method => "creditcard";

        public Task<(string TransactionId, string Provider, string Status)> PayAsync(
            decimal amount, int customerId, CancellationToken ct)
        {
            return Task.FromResult((Guid.NewGuid().ToString("N"), "CreditCard", "APPROVED"));
        }
    }
}