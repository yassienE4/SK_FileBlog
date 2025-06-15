using Microsoft.AspNetCore.Mvc;

namespace SkFileBlog.Features.Posts.Delete;

public static class DeletePostEndpoint
{
    public static void MapDeletePostEndpoint(this WebApplication app)
    {
        app.MapDelete("/api/posts/{id}", async (string id, 
                [FromServices] PostService postService,
                HttpContext context) =>
            {
                // Get the username from the authenticated user
                var username = context.User.Identity?.Name ?? "anonymous";
            
                // Delete the post
                var result = await postService.DeletePostAsync(id, username);
            
                if (!result)
                {
                    return Results.NotFound();
                }
            
                return Results.NoContent();
            })
            .RequireAuthorization("AuthorOrAdmin")
            .WithName("DeletePost")
            .WithDisplayName("Delete a blog post")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}