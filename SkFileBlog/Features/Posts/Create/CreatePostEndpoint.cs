using Microsoft.AspNetCore.Mvc;
using SkFileBlog.Shared.Models;

namespace SkFileBlog.Features.Posts.Create;

public static class CreatePostEndpoint
{
    public static void MapCreatePostEndpoint(this WebApplication app)
    {
        app.MapPost("/api/posts", async ([FromBody] CreatePostRequest request, 
                [FromServices] PostService postService,
                HttpContext context) =>
            {
                // Get the username from the authenticated user
                var username = context.User.Identity?.Name ?? "anonymous";
            
                // Convert request to BlogPost
                var post = new BlogPost
                {
                    Title = request.Title,
                    Slug = request.Slug,
                    Description = request.Description,
                    Content = request.Content,
                    Tags = request.Tags,
                    Categories = request.Categories,
                    FeaturedImage = request.FeaturedImage,
                    Status = request.Publish ? PublishStatus.Published : PublishStatus.Draft,
                    AuthorUsername = username
                };
            
                // Create the post
                var createdPost = await postService.CreatePostAsync(post, username);
            
                return Results.Created($"/api/posts/{createdPost.Slug}", createdPost);
            })
            .RequireAuthorization("AuthorOrAdmin")
            .WithName("CreatePost")
            .WithDisplayName("Create a new blog post")
            .Produces<BlogPost>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}