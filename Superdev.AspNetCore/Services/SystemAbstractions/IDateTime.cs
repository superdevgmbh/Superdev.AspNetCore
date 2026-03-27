namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public interface IDateTime
    {
        DateTime Now { get; }

        DateTime UtcNow { get; }
    }
}