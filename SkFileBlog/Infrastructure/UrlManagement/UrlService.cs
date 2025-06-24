using System.Text.RegularExpressions;
using SkFileBlog.Shared.Models;

namespace SkFileBlog.Infrastructure.UrlManagement;

public class UrlService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UrlService> _logger;
    private readonly string _baseUrl;
    
    public UrlService(IConfiguration configuration, ILogger<UrlService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["Blog:BaseUrl"]?.TrimEnd('/') ?? "";
    }
    
    public string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;
            
        // Convert to lowercase
        var slug = title.ToLowerInvariant();
        
        // Remove diacritics (accents)
        slug = RemoveDiacritics(slug);
        
        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s", "-");
        
        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);
        
        // Remove multiple hyphens
        slug = Regex.Replace(slug, @"-{2,}", "-");
        
        // Trim hyphens from start and end
        slug = slug.Trim('-');
        
        return slug;
    }
    
    public string GetPostUrl(string slug) => $"{_baseUrl}/blog/{slug}";
    
    public string GetCategoryUrl(string categorySlug) => $"{_baseUrl}/category/{categorySlug}";
    
    public string GetTagUrl(string tagSlug) => $"{_baseUrl}/tag/{tagSlug}";
    
    public string GetMediaUrl(string username, string filename) => $"{_baseUrl}/media/{username}/{filename}";
    
    public string GetUserProfileUrl(string username) => $"{_baseUrl}/author/{username}";
    
    public string GetCanonicalUrl(BlogPost post)
    {
        if (post.Status != PublishStatus.Published)
            return string.Empty;
            
        return GetPostUrl(post.Slug);
    }
    
    // Check if URLs need to be redirected (e.g., after slug changes)
    public bool NeedsRedirect(string oldSlug, string newSlug) => !string.IsNullOrEmpty(oldSlug) && 
                                                              !string.IsNullOrEmpty(newSlug) && 
                                                              oldSlug != newSlug;
    
    // Helper method to remove diacritics (accents)
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}