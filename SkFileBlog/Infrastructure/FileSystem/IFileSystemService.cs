namespace SkFileBlog.Infrastructure.FileSystem;

public interface IFileSystemService
{
    Task EnsureDirectoryExistsAsync(string path);
    bool DirectoryExists(string path);
    
    // File operations
    Task<string> ReadTextAsync(string path);
    Task WriteTextAsync(string path, string content);
    Task<bool> FileExistsAsync(string path);
    Task DeleteFileAsync(string path);
    
    // Blog-specific operations
    Task<IEnumerable<string>> ListPostFilesAsync();
    Task<IEnumerable<string>> ListFilesInDirectoryAsync(string path);
    
    // Path helpers
    string GetContentDirectory();
    string GetPostsDirectory();
    string GetMediaDirectory();
    string GetUsersDirectory();
    string CombinePath(params string[] paths);
}