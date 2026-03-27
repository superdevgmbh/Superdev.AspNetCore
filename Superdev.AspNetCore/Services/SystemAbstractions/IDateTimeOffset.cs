namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public interface IDateTimeOffset
    {
        DateTimeOffset Now { get; }

        DateTimeOffset UtcNow { get; }
    }
}
