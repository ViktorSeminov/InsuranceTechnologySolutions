using System.Net;
using System.Text.Json;
using Claims.Infrastructure.Exceptions;

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
            var (statusCode, message) = GetStatusCodeAndMessage(exception);
            context.Response.StatusCode = statusCode;

            var result = JsonSerializer.Serialize(new { error = message });
            return context.Response.WriteAsync(result);
        }

        private static (int StatusCode, string Message) GetStatusCodeAndMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException ae =>
                    ((int)HttpStatusCode.BadRequest, ae.Message),
                NotFoundException nfe =>
                    ((int)HttpStatusCode.NotFound, nfe.Message),
                InvalidOperationException =>
                    ((int)HttpStatusCode.BadRequest, "The request contained invalid data."),
                _ =>
                    ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };
        }
    }
}

