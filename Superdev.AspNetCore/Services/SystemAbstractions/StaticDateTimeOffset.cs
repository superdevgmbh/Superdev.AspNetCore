namespace Superdev.AspNetCore.Services.SystemAbstractions
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
