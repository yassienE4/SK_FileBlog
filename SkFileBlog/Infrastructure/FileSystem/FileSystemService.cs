using System.Text;
namespace SkFileBlog.Infrastructure.FileSystem;
public class FileSystemService : IFileSystemService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileSystemService> _logger;
    private readonly string _contentRoot;

    public FileSystemService(IConfiguration configuration, ILogger<FileSystemService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get the content root from configuration or use a default
        _contentRoot = _configuration["Blog:ContentRoot"] ?? Path.Combine(Directory.GetCurrentDirectory(), "BlogContent");
        
        // Ensure the root directory exists
        Directory.CreateDirectory(_contentRoot);
        
        // Create standard directories
        Directory.CreateDirectory(GetPostsDirectory());
        Directory.CreateDirectory(GetMediaDirectory());
        Directory.CreateDirectory(GetUsersDirectory());
    }

    public async Task EnsureDirectoryExistsAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogInformation("Creating directory: {Path}", path);
            Directory.CreateDirectory(path);
        }
        await Task.CompletedTask;
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public async Task<string> ReadTextAsync(string path)
    {
        if (!File.Exists(path))
        {
            _logger.LogWarning("File not found: {Path}", path);
            return string.Empty;
        }
        
        return await File.ReadAllTextAsync(path, Encoding.UTF8);
    }

    public async Task WriteTextAsync(string path, string content)
    {
        // Ensure the directory exists
        await EnsureDirectoryExistsAsync(Path.GetDirectoryName(path)!);
        
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        _logger.LogInformation("File written: {Path}", path);
    }

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public async Task DeleteFileAsync(string path)
    {
        if (await FileExistsAsync(path))
        {
            File.Delete(path);
            _logger.LogInformation("File deleted: {Path}", path);
        }
    }

    public async Task<IEnumerable<string>> ListPostFilesAsync()
    {
        return await Task.FromResult(Directory.GetFiles(GetPostsDirectory(), "*.md", SearchOption.AllDirectories));
    }

    public async Task<IEnumerable<string>> ListFilesInDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogWarning("Directory not found: {Path}", path);
            return Enumerable.Empty<string>();
        }
        
        return await Task.FromResult(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
    }

    public string GetContentDirectory() => _contentRoot;

    public string GetPostsDirectory() => Path.Combine(_contentRoot, "Posts");

    public string GetMediaDirectory() => Path.Combine(_contentRoot, "Media");

    public string GetUsersDirectory() => Path.Combine(_contentRoot, "Users");

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }
}