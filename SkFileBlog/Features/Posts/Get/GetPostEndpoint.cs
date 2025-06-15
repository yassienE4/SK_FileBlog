using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Shared.Models;

namespace SkFileBlog.Features.Posts.Get;

public static class GetPostEndpoint
{
    public static void MapGetPostEndpoint(this WebApplication app)
    {
        app.MapGet("/api/posts/{slug}", async (string slug, 
                [FromServices] PostService postService) =>
            {
                var post = await postService.GetPostBySlugAsync(slug);
            
                if (post == null)
                {
                    return Results.NotFound();
                }
            
                return Results.Ok(post);
            })
            .WithName("GetPost")
            .WithDisplayName("Get a blog post by slug")
            .Produces<BlogPost>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}