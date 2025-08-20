namespace ProvaPub.Services.Time
{
    public class UtcDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
