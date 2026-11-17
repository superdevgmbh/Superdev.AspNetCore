namespace Superdev.AspNetCore.Services
{
    public class StaticDateTimeOffset : IDateTimeOffset
    {
        public StaticDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            this.Now = dateTimeOffset;
        }

        public DateTimeOffset Now { get; }

        public DateTimeOffset UtcNow => this.Now.ToUniversalTime();
    }
}
