namespace Superdev.AspNetCore.Services
{
    public interface IDateTimeOffset
    {
        DateTimeOffset Now { get; }

        DateTimeOffset UtcNow { get; }
    }
}
