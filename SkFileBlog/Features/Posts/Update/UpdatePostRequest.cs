namespace SkFileBlog.Features.Posts.Update;

public class UpdatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string FeaturedImage { get; set; } = string.Empty;
    public bool Publish { get; set; } = false;
}