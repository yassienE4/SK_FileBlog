namespace SkFileBlog.Infrastructure.FileSystem;

public class DirectoryStructureInitializer
{
    public static async Task InitializeAsync(IFileSystemService fileSystem)
    {
        // Ensure core directories exist
        await fileSystem.EnsureDirectoryExistsAsync(fileSystem.GetPostsDirectory());
        await fileSystem.EnsureDirectoryExistsAsync(fileSystem.GetMediaDirectory());
        await fileSystem.EnsureDirectoryExistsAsync(fileSystem.GetUsersDirectory());
        
        // Create subdirectories for better organization
        await fileSystem.EnsureDirectoryExistsAsync(Path.Combine(fileSystem.GetPostsDirectory(), "Drafts"));
        await fileSystem.EnsureDirectoryExistsAsync(Path.Combine(fileSystem.GetPostsDirectory(), "Published"));
        await fileSystem.EnsureDirectoryExistsAsync(Path.Combine(fileSystem.GetMediaDirectory(), "Images"));
        await fileSystem.EnsureDirectoryExistsAsync(Path.Combine(fileSystem.GetMediaDirectory(), "Documents"));
    }
}