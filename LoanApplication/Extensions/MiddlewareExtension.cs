using Microsoft.AspNetCore.Builder;
using LoanApplication.Middleware;

namespace LoanApplication.Extensions
{

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseActivityLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ActivityLoggingMiddleware>();
        }
    }
}
