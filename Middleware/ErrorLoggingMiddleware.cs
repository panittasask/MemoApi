using System.Net;
using System.Text;
using System.Text.Json;

namespace MemmoApi.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private static readonly object _fileLock = new object();

        public ErrorLoggingMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                LogErrorToFile(context, ex);
                await HandleExceptionAsync(context, ex);
            }
        }

        private void LogErrorToFile(HttpContext context, Exception ex)
        {
            try
            {
                var logsDir = Path.Combine(_env.ContentRootPath, "logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                var fileName = $"error-{DateTime.Now:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(logsDir, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine($"Timestamp   : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"TraceId     : {context.TraceIdentifier}");
                sb.AppendLine($"Method      : {context.Request.Method}");
                sb.AppendLine($"Path        : {context.Request.Path}{context.Request.QueryString}");
                sb.AppendLine($"RemoteIP    : {context.Connection.RemoteIpAddress}");
                sb.AppendLine($"User        : {context.User?.Identity?.Name ?? "Anonymous"}");
                sb.AppendLine($"ExceptionType: {ex.GetType().FullName}");
                sb.AppendLine($"Message     : {ex.Message}");
                sb.AppendLine("StackTrace  :");
                sb.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    sb.AppendLine("InnerException:");
                    sb.AppendLine($"  Type    : {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"  Message : {ex.InnerException.Message}");
                    sb.AppendLine($"  Stack   : {ex.InnerException.StackTrace}");
                }

                sb.AppendLine("==================================================");
                sb.AppendLine();

                lock (_fileLock)
                {
                    File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Swallow logging errors so they never break the response pipeline
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                message = "An unexpected error occurred. Please try again later.",
                traceId = context.TraceIdentifier
            });

            return context.Response.WriteAsync(payload);
        }
    }
}
