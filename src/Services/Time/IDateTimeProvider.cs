namespace ProvaPub.Services.Time
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
