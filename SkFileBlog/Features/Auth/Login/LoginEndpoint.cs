using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Infrastructure.Authentication;
using SkFileBlog.Infrastructure.Authentication.Models;
using System.IdentityModel.Tokens.Jwt;

namespace SkFileBlog.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async ([FromBody] LoginRequest request,
                [FromServices] UserService userService,
                [FromServices] JwtProvider jwtProvider) =>
            {
                // Authenticate the user
                var token = await userService.AuthenticateAsync(request.Username, request.Password);
            
                if (token == null)
                {
                    return Results.Unauthorized();
                }
            
                // Get the user
                var user = await userService.GetUserAsync(request.Username);
                if (user == null)
                {
                    return Results.Unauthorized();
                }
            
                // Get token expiration
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var expiration = jwtToken.ValidTo;
            
                // Create response
                var response = new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Roles = user.Roles,
                    TokenExpiration = expiration
                };
            
                return Results.Ok(response);
            })
            .AllowAnonymous()
            .WithName("Login")
            .WithDisplayName("User login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}