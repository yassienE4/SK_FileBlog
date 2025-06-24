using System.Text.Json;
using SkFileBlog.Infrastructure.FileSystem;

namespace SkFileBlog.Infrastructure.UrlManagement;

public class RedirectService
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<RedirectService> _logger;
    private readonly string _redirectsPath;
    private Dictionary<string, RedirectEntry> _redirects = new();
    
    public class RedirectEntry
    {
        public string NewUrl { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 301;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public RedirectService(IFileSystemService fileSystem, ILogger<RedirectService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _redirectsPath = Path.Combine(_fileSystem.GetContentDirectory(), "redirects.json");
        
        // Load redirects on initialization
        LoadRedirectsAsync().GetAwaiter().GetResult();
    }
    
    public async Task LoadRedirectsAsync()
    {
        try
        {
            if (await _fileSystem.FileExistsAsync(_redirectsPath))
            {
                var json = await _fileSystem.ReadTextAsync(_redirectsPath);
                _redirects = JsonSerializer.Deserialize<Dictionary<string, RedirectEntry>>(json) 
                    ?? new Dictionary<string, RedirectEntry>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading redirects");
            _redirects = new Dictionary<string, RedirectEntry>();
        }
    }
    
    public async Task SaveRedirectsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_redirects, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await _fileSystem.WriteTextAsync(_redirectsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving redirects");
        }
    }
    
    public async Task AddRedirectAsync(string oldPath, string newPath, int statusCode = 301)
    {
        // Normalize paths
        oldPath = oldPath.TrimStart('/').ToLowerInvariant();
        newPath = newPath.TrimStart('/').ToLowerInvariant();
        
        // Add or update redirect
        _redirects[oldPath] = new RedirectEntry
        {
            NewUrl = newPath,
            StatusCode = statusCode,
            CreatedAt = DateTime.UtcNow
        };
        
        await SaveRedirectsAsync();
    }
    
    public async Task RemoveRedirectAsync(string path)
    {
        path = path.TrimStart('/').ToLowerInvariant();
        
        if (_redirects.ContainsKey(path))
        {
            _redirects.Remove(path);
            await SaveRedirectsAsync();
        }
    }
    
    public RedirectEntry? GetRedirect(string path)
    {
        path = path.TrimStart('/').ToLowerInvariant();
        return _redirects.TryGetValue(path, out var redirect) ? redirect : null;
    }
}