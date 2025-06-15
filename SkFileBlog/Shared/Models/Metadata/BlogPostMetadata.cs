namespace SkFileBlog.Shared.Models.Metadata;

public class BlogPostMetadata
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string FeaturedImage { get; set; } = string.Empty;
    
    // File paths
    public string ContentFilePath { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    
    public static BlogPostMetadata FromBlogPost(BlogPost post)
    {
        return new BlogPostMetadata
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Description = post.Description,
            AuthorUsername = post.AuthorUsername,
            AuthorDisplayName = post.AuthorDisplayName,
            CreatedAt = post.CreatedAt,
            PublishedAt = post.PublishedAt,
            ModifiedAt = post.ModifiedAt,
            Status = post.Status,
            Tags = post.Tags,
            Categories = post.Categories,
            FeaturedImage = post.FeaturedImage
        };
    }
}