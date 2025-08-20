namespace ProvaPub.Services.Payments
{
    public interface IPaymentProcessor
    {
        string Method { get; }
        Task<(string TransactionId, string Provider, string Status)> PayAsync(
            decimal amount, int customerId, CancellationToken ct);
    }
}
