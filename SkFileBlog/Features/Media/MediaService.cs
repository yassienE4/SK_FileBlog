using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SkFileBlog.Infrastructure.FileSystem;

namespace SkFileBlog.Features.Media;

public class MediaService
{
    private readonly IFileSystemService _fileSystem;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaService> _logger;
    
    public MediaService(
        IFileSystemService fileSystem,
        IConfiguration configuration,
        ILogger<MediaService> logger)
    {
        _fileSystem = fileSystem;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<(string FileName, string FilePath, string Url, long FileSize, string ContentType)> 
        UploadFileAsync(IFormFile file, string username, bool resizeImage = false)
    {
        try
        {
            // Create user-specific media directory
            var userMediaDir = Path.Combine(_fileSystem.GetMediaDirectory(), username);
            await _fileSystem.EnsureDirectoryExistsAsync(userMediaDir);
            
            // Generate a unique filename to avoid collisions
            var originalFileName = Path.GetFileName(file.FileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
            
            // Create full path
            var filePath = Path.Combine(userMediaDir, uniqueFileName);
            
            // Handle image resizing if requested
            if (resizeImage && IsImageFile(extension))
            {
                await ResizeAndSaveImageAsync(file, filePath);
            }
            else
            {
                // Save regular file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            
            // Determine URL
            var baseUrl = _configuration["Blog:BaseUrl"] ?? "/";
            var url = $"{baseUrl.TrimEnd('/')}/media/{username}/{uniqueFileName}";
            
            return (uniqueFileName, filePath, url, file.Length, file.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
            throw;
        }
    }
    
    private async Task ResizeAndSaveImageAsync(IFormFile file, string outputPath)
    {
        // Get max dimensions from config
        var maxWidth = _configuration.GetValue<int>("Media:MaxWidth", 1920);
        var maxHeight = _configuration.GetValue<int>("Media:MaxHeight", 1080);
        
        using var image = await Image.LoadAsync(file.OpenReadStream());
        
        // Only resize if the image is larger than the max dimensions
        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(maxWidth, maxHeight),
                Mode = ResizeMode.Max
            }));
        }
        
        await image.SaveAsync(outputPath);
    }
    
    public async Task<IEnumerable<MediaFileInfo>> ListUserMediaAsync(string username)
    {
        var userMediaDir = Path.Combine(_fileSystem.GetMediaDirectory(), username);
        if (!_fileSystem.DirectoryExists(userMediaDir))
        {
            return Enumerable.Empty<MediaFileInfo>();
        }
        
        var files = await _fileSystem.ListFilesInDirectoryAsync(userMediaDir);
        var baseUrl = _configuration["Blog:BaseUrl"] ?? "/";
        
        return files.Select(filePath =>
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            
            return new MediaFileInfo
            {
                FileName = fileName,
                FilePath = filePath,
                Url = $"{baseUrl.TrimEnd('/')}/media/{username}/{fileName}",
                FileSize = fileInfo.Length,
                ContentType = GetContentTypeFromExtension(Path.GetExtension(fileName)),
                LastModified = fileInfo.LastWriteTimeUtc
            };
        }).OrderByDescending(f => f.LastModified);
    }
    
    public async Task<bool> DeleteMediaFileAsync(string filePath, string username)
    {
        try
        {
            // Security check - make sure the file belongs to the user
            var userMediaDir = Path.Combine(_fileSystem.GetMediaDirectory(), username);
            if (!filePath.StartsWith(userMediaDir))
            {
                _logger.LogWarning("User {Username} attempted to delete file outside their media directory", username);
                return false;
            }
            
            if (!await _fileSystem.FileExistsAsync(filePath))
            {
                return false;
            }
            
            await _fileSystem.DeleteFileAsync(filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media file {FilePath}", filePath);
            return false;
        }
    }
    
    private bool IsImageFile(string extension)
    {
        return new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }
            .Contains(extension.ToLowerInvariant());
    }
    
    private string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/msword",
            ".xls" or ".xlsx" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }
}

public class MediaFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}