namespace ProvaPub.Models
{
    [Obsolete("Use PagedResult<Customer> no lugar de CustomerList.")]
    public class CustomerList
	{
		public List<Customer> Customers { get; set; }
		public int TotalCount { get; set; }
		public bool HasNext { get; set; }
	}
}
