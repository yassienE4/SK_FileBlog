using SkFileBlog.Shared.Models;

namespace SkFileBlog.Features.Posts.List;

public class ListPostsResponse
{
    public List<BlogPost> Posts { get; set; } = new();
    public int TotalPosts { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}