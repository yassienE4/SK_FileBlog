namespace SkFileBlog.Shared.Models;

public class BlogPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string FeaturedImage { get; set; } = string.Empty;
}
public enum PublishStatus
{
    Draft,
    Published,
    Scheduled
}