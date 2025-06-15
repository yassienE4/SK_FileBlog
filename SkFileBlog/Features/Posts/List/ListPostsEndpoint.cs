using Microsoft.AspNetCore.Mvc;

namespace SkFileBlog.Features.Posts.List;

public static class ListPostsEndpoint
{
    public static void MapListPostsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/posts", async ([AsParameters] ListPostsRequest request, 
                [FromServices] PostService postService,
                HttpContext context) =>
            {
                // Check if user is authenticated to determine if drafts should be included
                var includeDrafts = request.IncludeDrafts && context.User.Identity?.IsAuthenticated == true;
            
                // Get posts
                var (posts, totalCount) = await postService.ListPostsAsync(
                    request.Page,
                    request.PageSize,
                    request.Category,
                    request.Tag,
                    request.Author,
                    includeDrafts,
                    request.SearchQuery,
                    request.SortBy ?? "PublishedAt",
                    request.Descending);
            
                // Calculate if there are more pages
                var hasMore = request.Page * request.PageSize < totalCount;
            
                var response = new ListPostsResponse
                {
                    Posts = posts.ToList(),
                    TotalPosts = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    HasMore = hasMore
                };
            
                return Results.Ok(response);
            })
            .WithName("ListPosts")
            .WithDisplayName("List blog posts with optional filtering and pagination")
            .Produces<ListPostsResponse>(StatusCodes.Status200OK);
    }
}