namespace SkFileBlog.Shared.Models.Metadata;

public class SiteMetadata
{
    public string SiteName { get; set; } = "My Blog";
    public string Description { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "/";
    public int PostsPerPage { get; set; } = 10;
    public List<string> AllTags { get; set; } = new();
    public List<string> AllCategories { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}