namespace Superdev.AspNetCore.Services
{
    public interface IDateTime
    {
        DateTime Now { get; }

        DateTime UtcNow { get; }
    }
}