namespace Sustain.Utilities.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the error
            _logger.LogError(exception, "An unhandled exception occurred. Path: {Path}", context.Request.Path);

            // Log detailed error info
            _logger.LogError("Exception Details: Message={Message}, StackTrace={StackTrace}, Source={Source}",
                exception.Message,
                exception.StackTrace,
                exception.Source);

            // Set response
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Return error page or JSON based on request type
            if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "An error occurred while processing your request.",
                    message = exception.Message,
                    statusCode = 500
                });
            }
            else
            {
                // Redirect to error page
                context.Response.Redirect("/Home/Error");
            }
        }
    }

}
