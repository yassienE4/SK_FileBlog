namespace SkFileBlog.Infrastructure.Markdown;

public interface IMarkdownProcessor
{
    /// <summary>
    /// Converts Markdown text to HTML
    /// </summary>
    string ToHtml(string markdown);
    
    /// <summary>
    /// Extracts the front matter metadata from a Markdown document
    /// </summary>
    Dictionary<string, string> ExtractFrontMatter(string markdown, out string cleanedMarkdown);
    
    /// <summary>
    /// Processes image references to convert relative paths to proper URLs
    /// </summary>
    string ProcessImagePaths(string html, string baseUrl);
}