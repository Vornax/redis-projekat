using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TrendingApi.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;

        public AuthorizationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _apiKey = configuration["Authorization:ApiKey"] ?? "trending123";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Preskoči Swagger i GET zahteve
            if (context.Request.Path.StartsWithSegments("/swagger") || context.Request.Method == "GET")
            {
                await _next(context);
                return;
            }

            // Čita API ključ iz Authorization header-a
            var apiKey = context.Request.Headers["X-API-Key"].ToString();

            if (string.IsNullOrEmpty(apiKey) || apiKey != _apiKey)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                return;
            }

            await _next(context);
        }
    }
}