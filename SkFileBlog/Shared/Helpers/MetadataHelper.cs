using System.Text.Json;
using SkFileBlog.Infrastructure.FileSystem;
using SkFileBlog.Shared.Models.Metadata;

namespace SkFileBlog.Shared.Helpers;

public class MetadataHelper
{
    private readonly IFileSystemService _fileSystem;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public MetadataHelper(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    public async Task<T?> ReadMetadataAsync<T>(string path) where T : class
    {
        if (!await _fileSystem.FileExistsAsync(path))
        {
            return null;
        }
        
        var json = await _fileSystem.ReadTextAsync(path);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
    
    public async Task WriteMetadataAsync<T>(string path, T metadata) where T : class
    {
        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        await _fileSystem.WriteTextAsync(path, json);
    }
    
    public string GetMetadataPath(BlogPostMetadata postMetadata)
    {
        return Path.Combine(postMetadata.DirectoryPath, "meta.json");
    }
    
    public string GetSiteMetadataPath()
    {
        return Path.Combine(_fileSystem.GetContentDirectory(), "site.json");
    }
    
    public string GetCategoryMetadataPath(string categorySlug)
    {
        var categoriesDir = Path.Combine(_fileSystem.GetContentDirectory(), "categories");
        return Path.Combine(categoriesDir, $"{categorySlug}.json");
    }
    
    public string GetTagMetadataPath(string tagSlug)
    {
        var tagsDir = Path.Combine(_fileSystem.GetContentDirectory(), "tags");
        return Path.Combine(tagsDir, $"{tagSlug}.json");
    }
}