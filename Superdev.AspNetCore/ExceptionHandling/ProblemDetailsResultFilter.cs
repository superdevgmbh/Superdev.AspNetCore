using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace Superdev.AspNetCore.ExceptionHandling
{
    public sealed class ProblemDetailsResultFilter : IAsyncResultFilter
    {
        private readonly IProblemDetailsService problemDetailsService;
        private readonly bool isDevelopment;

        public ProblemDetailsResultFilter(
            IProblemDetailsService problemDetailsService,
            IHostEnvironment hostEnvironment)
        {
            this.problemDetailsService = problemDetailsService;
            this.isDevelopment = hostEnvironment.IsDevelopment();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult &&
                objectResult.StatusCode is >= 400 &&
                objectResult.Value is not ProblemDetails)
            {
                var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;

                var problemDetails = new ProblemDetails
                {
                    Type = null,
                    Status = statusCode,
                    Title = ReasonPhrases.GetReasonPhrase(statusCode),
                    Detail = this.isDevelopment ? objectResult.Value?.ToString() : null,
                    Instance = context.HttpContext.Request.Path
                };

                var problemDetailsContext = new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = problemDetails
                };

                context.Result = new EmptyResult();

                context.HttpContext.Response.StatusCode = statusCode;

                await this.problemDetailsService.TryWriteAsync(problemDetailsContext);
            }
            else
            {
                await next();
            }
        }
    }

}
