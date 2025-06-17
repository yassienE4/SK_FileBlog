using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Infrastructure.Authentication;

namespace SkFileBlog.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void MapRegisterEndpoint(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async ([FromBody] RegisterRequest request,
                                            [FromServices] UserService userService,
                                            [FromServices] ILogger<Program> logger,
                                            HttpContext context) =>
        {
            // Check if user creation is restricted to admins
            var requireAdmin = app.Configuration.GetValue<bool>("Auth:RequireAdminForRegistration", false);
            
            if (requireAdmin)
            {
                // Check if the current user is an admin
                var isAdmin = context.User.IsInRole("Admin");
                if (!isAdmin)
                {
                    logger.LogWarning("Non-admin user attempted to register a new user when admin is required");
                    return Results.Forbid();
                }
            }
            
            // Default role for new users
            var roles = new List<string> { "Author" };
            
            // Create the user
            var result = await userService.CreateUserAsync(
                request.Username,
                request.Email,
                request.DisplayName,
                request.Password,
                roles);
                
            if (!result)
            {
                logger.LogWarning("Failed to create user {Username}", request.Username);
                return Results.BadRequest("Username already exists or could not create user");
            }
            
            // Return success
            return Results.Created($"/api/users/{request.Username}", new
            {
                Username = request.Username,
                DisplayName = request.DisplayName,
                Email = request.Email,
                Roles = roles
            });
        })
        .WithName("Register")
        .WithDisplayName("Register a new user")
        .Produces<object>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}