using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sustain.Utilities.Middleware
{
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _sessionTimeoutMinutes;

        public SessionTimeoutMiddleware(RequestDelegate next, int sessionTimeoutMinutes = 120)
        {
            _next = next;
            _sessionTimeoutMinutes = sessionTimeoutMinutes;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var lastActivity = context.Session.GetString("LastActivityTime");

                if (!string.IsNullOrEmpty(lastActivity))
                {
                    if (DateTime.TryParse(lastActivity, out DateTime lastActivityTime))
                    {
                        var timeSinceLastActivity = DateTime.UtcNow - lastActivityTime;

                        if (timeSinceLastActivity.TotalMinutes > _sessionTimeoutMinutes)
                        {
                            // Clear session and redirect to login
                            context.Session.Clear();
                            context.Response.Redirect("/Auth/Login?expired=true");
                            return;
                        }
                    }
                }

                // Update last activity time
                context.Session.SetString("LastActivityTime", DateTime.UtcNow.ToString("o"));
            }

            await _next(context);
        }
    }

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionTimeout(this IApplicationBuilder builder, int timeoutMinutes = 120)
        {
            return builder.UseMiddleware<SessionTimeoutMiddleware>(timeoutMinutes);
        }

        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}