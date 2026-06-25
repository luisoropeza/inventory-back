using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Middlewares
{
    public class BusinessIdValidationMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var headerBusinessId = context.Request.Headers["businessId"].FirstOrDefault();
                if (headerBusinessId is not null)
                {
                    var claimBusinessId = context.User.Claims
                        .FirstOrDefault(c => c.Type == "businessId")?.Value;

                    if (!string.Equals(headerBusinessId, claimBusinessId, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "Forbidden",
                            Detail = "Business ID does not match the authenticated user's business."
                        });
                        return;
                    }
                }
            }

            await next(context);
        }
    }
}
