using System.Net;

namespace api.Middlewares
{
    public class RequestValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only check content type for methods that typically have a body
            if (IsMethodWithBody(context.Request.Method))
            {
                // Allow multipart/form-data for specific endpoints
                var path = context.Request.Path.Value?.ToLower();
                if (path != null)
                {
                    await _next(context);
                    return;
                }

                if (context.Request.ContentLength > 0 && !context.Request.HasJsonContentType())
                {
                    context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Request content type must be application/json or multipart/form-data"
                    });
                    return;
                }

                if (context.Request.ContentLength > 1_000_000) // 1MB limit
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Request body too large"
                    });
                    return;
                }
            }

            await _next(context);
        }

        private bool IsMethodWithBody(string method)
        {
            return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
        }
    }
}