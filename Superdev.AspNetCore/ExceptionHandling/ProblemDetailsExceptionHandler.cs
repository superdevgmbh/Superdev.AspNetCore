using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Superdev.AspNetCore.ExceptionHandling
{
    public class ProblemDetailsExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService problemDetailsService;
        private readonly bool isDevelopment;

        public ProblemDetailsExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IHostEnvironment hostEnvironment)
        {
            this.problemDetailsService = problemDetailsService;
            this.isDevelopment = hostEnvironment.IsDevelopment();
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var statusCode = exception switch
            {
                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            httpContext.Response.StatusCode = statusCode;

            IDictionary<string, object?> extension = new Dictionary<string, object?>
            {
                { "exception", exception.GetType().FullName }
            };

            if (this.isDevelopment)
            {
                extension.Add("stackTrace", exception.StackTrace);
            }

            if (exception is ArgumentException argumentException)
            {
                extension.Add("paramName", argumentException.ParamName);
            }

            var problemDetails = new ProblemDetails
            {
                Type = null,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Status = statusCode,
                Detail = this.isDevelopment ? exception.Message : null,
                Instance = httpContext.Request.Path,
                Extensions = extension,
            };

            var problemDetailsContext = new ProblemDetailsContext
            {
                Exception = exception,
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            };

            return this.problemDetailsService.TryWriteAsync(problemDetailsContext);
        }
    }
}
