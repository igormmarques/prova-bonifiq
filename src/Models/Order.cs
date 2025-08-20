namespace ProvaPub.Models
{
	public class Order
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public Customer Customer { get; set; }

        // Novos campos para armazenar detalhes do pagamento
        public string PaymentMethod { get; set; }
        public string PaymentProvider { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentTransactionId { get; set; }
    }
}
