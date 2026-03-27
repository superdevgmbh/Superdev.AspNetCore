namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public class StaticDateTime : IDateTime
    {
        public StaticDateTime(DateTime dateTime)
        {
            this.Now = dateTime;
        }

        public DateTime Now { get; }

        public DateTime UtcNow
        {
            get { return this.Now.ToUniversalTime(); }
        }
    }
}