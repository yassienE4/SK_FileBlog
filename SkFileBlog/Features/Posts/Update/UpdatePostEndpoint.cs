using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Shared.Models;

namespace SkFileBlog.Features.Posts.Update;

public static class UpdatePostEndpoint
{
    public static void MapUpdatePostEndpoint(this WebApplication app)
    {
        app.MapPut("/api/posts/{id}", async (string id, 
                [FromBody] UpdatePostRequest request, 
                [FromServices] PostService postService,
                HttpContext context) =>
            {
                // Get the username from the authenticated user
                var username = context.User.Identity?.Name ?? "anonymous";
            
                // Convert request to BlogPost with updated values
                var post = new BlogPost
                {
                    Title = request.Title,
                    Slug = request.Slug,
                    Description = request.Description,
                    Content = request.Content,
                    Tags = request.Tags,
                    Categories = request.Categories,
                    FeaturedImage = request.FeaturedImage,
                    Status = request.Publish ? PublishStatus.Published : PublishStatus.Draft
                };
            
                // Update the post
                var updatedPost = await postService.UpdatePostAsync(id, post, username);
            
                if (updatedPost == null)
                {
                    return Results.NotFound();
                }
            
                return Results.Ok(updatedPost);
            })
            .RequireAuthorization("AuthorOrAdmin")
            .WithName("UpdatePost")
            .WithDisplayName("Update an existing blog post")
            .Produces<BlogPost>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}