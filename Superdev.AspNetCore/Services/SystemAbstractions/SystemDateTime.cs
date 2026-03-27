namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public class SystemDateTime : IDateTime
    {
        DateTime IDateTime.Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}