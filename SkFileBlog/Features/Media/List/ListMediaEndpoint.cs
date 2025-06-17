using Microsoft.AspNetCore.Mvc;

namespace SkFileBlog.Features.Media.List;

public static class ListMediaEndpoint
{
    public static void MapListMediaEndpoint(this WebApplication app)
    {
        app.MapGet("/api/media", async (HttpContext context,
                [FromServices] MediaService mediaService) =>
            {
                // Get username from authenticated user
                var username = context.User.Identity?.Name ?? "anonymous";
            
                if (username == "anonymous")
                {
                    return Results.Unauthorized();
                }
            
                var files = await mediaService.ListUserMediaAsync(username);
                return Results.Ok(files);
            })
            .RequireAuthorization()
            .WithName("ListMedia")
            .WithDisplayName("List user media files")
            .Produces<IEnumerable<MediaFileInfo>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}