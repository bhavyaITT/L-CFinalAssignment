using System.Net;
using System.Text.Json;

namespace PRM.API
{
    /// <summary>
    /// Catches any unhandled exception from the entire request pipeline.
    /// Returns a consistent JSON error shape instead of letting ASP.NET's default
    /// yellow screen or HTML errors leak to API consumers.
    /// This is the Separation of Concerns principle — error handling is not scattered
    /// across controllers; it lives in one place.
    /// </summary>
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await WriteErrorResponseAsync(context, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                statusCode = 500,
                message = "An unexpected error occurred. Please try again later.",
                // Only expose details in Development to avoid leaking internal info
                detail = context.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsDevelopment() ? ex.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
