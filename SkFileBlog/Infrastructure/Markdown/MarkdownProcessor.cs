using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace SkFileBlog.Infrastructure.Markdown;

public class MarkdownProcessor : IMarkdownProcessor
{
    private readonly MarkdownPipeline _pipeline;
    private readonly ILogger<MarkdownProcessor> _logger;
    
    public MarkdownProcessor(ILogger<MarkdownProcessor> logger)
    {
        _logger = logger;
        
        // Configure Markdig with available extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions() 
            .UseAutoLinks()
            .UseGenericAttributes()
            .UseTaskLists()
            .UseEmphasisExtras()
            .UsePipeTables()
            .UseGridTables()
            .UseMediaLinks()
            .Build();
    }
    
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }
        
        try
        {
            return Markdig.Markdown.ToHtml(markdown, _pipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting markdown to HTML");
            return $"<p>Error processing markdown: {ex.Message}</p>";
        }
    }
    
    public Dictionary<string, string> ExtractFrontMatter(string markdown, out string cleanedMarkdown)
    {
        var result = new Dictionary<string, string>();
        cleanedMarkdown = markdown;
    
        if (string.IsNullOrEmpty(markdown))
        {
            return result;
        }
    
        try
        {
            // Check if the document starts with front matter delimiter
            if (markdown.StartsWith("---"))
            {
                // Find the closing front matter delimiter
                var endIndex = markdown.IndexOf("---", 3);
                if (endIndex > 0)
                {
                    // Extract the YAML content between the delimiters
                    var yaml = markdown.Substring(3, endIndex - 3).Trim();
                
                    // Parse YAML
                    var deserializer = new DeserializerBuilder().Build();
                    var dict = deserializer.Deserialize<Dictionary<string, object>>(yaml);
                
                    // Convert to string dictionary
                    foreach (var kvp in dict)
                    {
                        result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                
                    // Find the line after the closing delimiter
                    var lineEnd = markdown.IndexOf('\n', endIndex);
                    if (lineEnd > 0)
                    {
                        cleanedMarkdown = markdown.Substring(lineEnd + 1);
                    }
                    else
                    {
                        cleanedMarkdown = string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting front matter from markdown");
        }
    
        return result;
    }
    
    public string ProcessImagePaths(string html, string baseUrl)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }
        
        try
        {
            // Process <img> tags to ensure paths are correct
            // Replace relative paths that don't start with http:// or https:// with baseUrl
            var pattern = @"(<img\s+[^>]*src=[""])(?!http:|https:)(.*?)([""'][^>]*>)";
            return Regex.Replace(html, pattern, m => 
            {
                var prefix = m.Groups[1].Value;
                var path = m.Groups[2].Value.TrimStart('/');
                var suffix = m.Groups[3].Value;
                
                return $"{prefix}{baseUrl.TrimEnd('/')}/{path}{suffix}";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image paths");
            return html;
        }
    }
}