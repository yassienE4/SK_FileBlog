using Microsoft.AspNetCore.Mvc;

namespace SkFileBlog.Features.Media.Delete;

public static class DeleteMediaEndpoint
{
    public static void MapDeleteMediaEndpoint(this WebApplication app)
    {
        app.MapDelete("/api/media/{filePath}", async (string filePath,
                [FromServices] MediaService mediaService,
                HttpContext context) =>
            {
                // Get username from authenticated user
                var username = context.User.Identity?.Name ?? "anonymous";
            
                if (username == "anonymous")
                {
                    return Results.Unauthorized();
                }
            
                // Decode the URL-encoded path
                filePath = System.Web.HttpUtility.UrlDecode(filePath);
            
                // Security check - make sure the path is within the media directory
                var mediaDir = Path.Combine(app.Configuration["Blog:ContentRoot"] ?? "BlogContent", "Media");
                var fullPath = Path.Combine(mediaDir, filePath);
            
                var result = await mediaService.DeleteMediaFileAsync(fullPath, username);
            
                if (!result)
                {
                    return Results.NotFound();
                }
            
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("DeleteMedia")
            .WithDisplayName("Delete a media file")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}