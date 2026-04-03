namespace Superdev.AspNetCore.Services
{
    public class SystemDateTimeOffset : IDateTimeOffset
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
