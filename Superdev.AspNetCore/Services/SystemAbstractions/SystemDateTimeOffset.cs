namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public class SystemDateTimeOffset : IDateTimeOffset
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
