namespace Claims.Infrastructure.ExceptionHendlers
{
    public static class ExceptionHandlerExtentions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHalndler>();
        }
    }
}
