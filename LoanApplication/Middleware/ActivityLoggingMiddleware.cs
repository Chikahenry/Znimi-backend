using LoanApplication.Services;
using System.Security.Claims;

namespace LoanApplication.Middleware
{
    public class ActivityLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            // Log activity for authenticated users on specific endpoints
            if (context.User.Identity?.IsAuthenticated == true && ShouldLogActivity(path, method))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var activityType = DetermineActivityType(path, method);
                    var description = $"{method} request to {path}";
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                    try
                    {
                        await userService.LogActivity(userId, activityType, description);
                    }
                    catch
                    {
                        // Silently fail - don't block the request
                    }
                }
            }

            await _next(context);
        }

        private bool ShouldLogActivity(string path, string method)
        {
            // Log these activities
            var pathsToLog = new[]
            {
                "/api/loans",
                "/api/payments",
                "/api/borrowers",
                "/api/analytics",
                "/api/audit"
            };

            // Don't log GET requests to list endpoints to reduce noise
            if (method == "GET" && !path.Contains("/api/users/"))
                return false;

            return pathsToLog.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private string DetermineActivityType(string path, string method)
        {
            if (path.Contains("/loans") && method == "POST") return "CreateLoan";
            if (path.Contains("/loans") && method == "PUT") return "UpdateLoan";
            if (path.Contains("/payments") && method == "POST") return "RecordPayment";
            if (path.Contains("/borrowers") && method == "POST") return "CreateBorrower";
            if (path.Contains("/borrowers") && method == "PUT") return "UpdateBorrower";
            if (path.Contains("/analytics")) return "ViewAnalytics";
            if (path.Contains("/audit")) return "ViewAuditLog";

            return "APIRequest";
        }
    }
}
