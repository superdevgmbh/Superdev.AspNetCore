using Microsoft.AspNetCore.Mvc;
using Superdev.AspNetCore.ApplicationModelConventions;

namespace Superdev.AspNetCore.Sample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnvironmentRestricted("Development")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet]
        public ActionResult<IReadOnlyCollection<WeatherForecast>> Get()
        {
            var forecast = Enumerable.Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    Summaries[Random.Shared.Next(Summaries.Length)]))
                .ToArray();

            return this.Ok(forecast);
        }
    }

    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(this.TemperatureC / 0.5556);
    }
}
