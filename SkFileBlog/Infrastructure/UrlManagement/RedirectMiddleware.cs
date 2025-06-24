namespace SkFileBlog.Infrastructure.UrlManagement;

public class RedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RedirectMiddleware> _logger;
    
    public RedirectMiddleware(RequestDelegate next, ILogger<RedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, RedirectService redirectService)
    {
        var path = context.Request.Path.Value?.TrimStart('/');
        
        // Skip if path is null or API endpoints
        if (string.IsNullOrEmpty(path) || path.StartsWith("api/"))
        {
            await _next(context);
            return;
        }
        
        // Check if path has a redirect
        var redirect = redirectService.GetRedirect(path);
        
        if (redirect != null)
        {
            _logger.LogInformation("Redirecting {OldPath} to {NewPath}", path, redirect.NewUrl);
            
            // Perform redirect
            context.Response.StatusCode = redirect.StatusCode;
            context.Response.Headers.Add("Location", $"/{redirect.NewUrl}");
            return;
        }
        
        // No redirect, continue pipeline
        await _next(context);
    }
}

// Extension method for middleware registration
public static class RedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseUrlRedirects(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RedirectMiddleware>();
    }
}