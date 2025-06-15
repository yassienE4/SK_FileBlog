using SkFileBlog.Infrastructure.FileSystem;

namespace SkFileBlog.Infrastructure.Markdown;

public class BlogPostProcessor
{
    private readonly IMarkdownProcessor _markdownProcessor;
    private readonly IFileSystemService _fileSystem;
    private readonly IConfiguration _configuration;
    
    public BlogPostProcessor(
        IMarkdownProcessor markdownProcessor,
        IFileSystemService fileSystem,
        IConfiguration configuration)
    {
        _markdownProcessor = markdownProcessor;
        _fileSystem = fileSystem;
        _configuration = configuration;
    }
    
    public async Task<(Dictionary<string, string> Metadata, string Content)> ProcessPostFileAsync(string filePath)
    {
        // Read the markdown content
        var markdown = await _fileSystem.ReadTextAsync(filePath);
        
        // Extract front matter
        var metadata = _markdownProcessor.ExtractFrontMatter(markdown, out var cleanedMarkdown);
        
        // Convert to HTML
        var html = _markdownProcessor.ToHtml(cleanedMarkdown);
        
        // Process image paths
        var baseUrl = _configuration["Blog:BaseUrl"] ?? "/";
        var processedHtml = _markdownProcessor.ProcessImagePaths(html, baseUrl);
        
        return (metadata, processedHtml);
    }
}