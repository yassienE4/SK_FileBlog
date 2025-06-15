namespace SkFileBlog.Features.Posts.List;

public class ListPostsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public string? Author { get; set; }
    public bool IncludeDrafts { get; set; } = false;
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; } = "PublishedAt";
    public bool Descending { get; set; } = true;
}