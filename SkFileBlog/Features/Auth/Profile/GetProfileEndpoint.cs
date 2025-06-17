using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Infrastructure.Authentication;

namespace SkFileBlog.Features.Auth.Profile;

public static class GetProfileEndpoint
{
    public static void MapGetProfileEndpoint(this WebApplication app)
    {
        app.MapGet("/api/auth/profile", async (HttpContext context,
                [FromServices] UserService userService) =>
            {
                var username = context.User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Results.Unauthorized();
                }
            
                var user = await userService.GetUserAsync(username);
                if (user == null)
                {
                    return Results.NotFound();
                }
            
                return Results.Ok(new
                {
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Roles = user.Roles,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin
                });
            })
            .RequireAuthorization()
            .WithName("GetProfile")
            .WithDisplayName("Get current user profile")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}