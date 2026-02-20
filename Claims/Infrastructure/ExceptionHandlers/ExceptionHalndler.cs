using System.Net;
using System.Text.Json;

namespace Claims.Infrastructure.ExceptionHendlers
{
    public class ExceptionHalndler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHalndler> _logger;

        public ExceptionHalndler(RequestDelegate next, ILogger<ExceptionHalndler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            switch (exception)
            {
                case ArgumentException ae:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = JsonSerializer.Serialize(new { Error = "Internal server error" });
            return context.Response.WriteAsync(result);
        }
    }
}

