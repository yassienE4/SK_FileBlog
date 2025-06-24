using System.Text.Json;
using SkFileBlog.Infrastructure.FileSystem;
using SkFileBlog.Infrastructure.UrlManagement;
using SkFileBlog.Shared.Models;
using SkFileBlog.Shared.Models.Metadata;
using SkFileBlog.Shared.Helpers;

namespace SkFileBlog.Features.Posts;

public class PostService
{
    private readonly IFileSystemService _fileSystem;
    private readonly MetadataHelper _metadataHelper;
    private readonly UrlService _urlService;       
    private readonly RedirectService _redirectService;
    private readonly ILogger<PostService> _logger;
    
    public PostService(
        IFileSystemService fileSystem, 
        MetadataHelper metadataHelper,
        UrlService urlService, 
        RedirectService redirectService,
        ILogger<PostService> logger)
    {
        _fileSystem = fileSystem;
        _metadataHelper = metadataHelper;
        _urlService = urlService;
        _redirectService = redirectService;
        _logger = logger;
    }
    
    public async Task<BlogPost> CreatePostAsync(BlogPost post, string authorUsername)
    {
        try
        {
            // Fill in some details
            post.Id = Guid.NewGuid().ToString();
            post.AuthorUsername = authorUsername;
            post.CreatedAt = DateTime.UtcNow;
            post.ModifiedAt = post.CreatedAt;
            
            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(post.Slug))
            {
                post.Slug = _urlService.GenerateSlug(post.Title);
            }
            
            // Determine status
            if (post.Status == PublishStatus.Published)
            {
                post.PublishedAt = DateTime.UtcNow;
            }
            
            // Create directory for the post
            var postDirectory = GetPostDirectory(post);
            await _fileSystem.EnsureDirectoryExistsAsync(postDirectory);
            
            // Save content to a markdown file
            var contentPath = Path.Combine(postDirectory, "content.md");
            await _fileSystem.WriteTextAsync(contentPath, post.Content);
            
            // Create metadata
            var metadata = BlogPostMetadata.FromBlogPost(post);
            metadata.ContentFilePath = contentPath;
            metadata.DirectoryPath = postDirectory;
            
            // Save metadata to JSON
            var metadataPath = _metadataHelper.GetMetadataPath(metadata);
            await _metadataHelper.WriteMetadataAsync(metadataPath, metadata);
            
            // Update site metadata (tags and categories)
            await UpdateSiteMetadataAsync(post);
            
            return post;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog post: {Title}", post.Title);
            throw;
        }
    }
    
    private string GetPostDirectory(BlogPost post)
    {
        var postsDirectory = post.Status == PublishStatus.Published 
            ? Path.Combine(_fileSystem.GetPostsDirectory(), "Published")
            : Path.Combine(_fileSystem.GetPostsDirectory(), "Drafts");
            
        return Path.Combine(postsDirectory, post.Id);
    }
    
    private string GenerateSlug(string title)
    {
        // Convert to lowercase and replace spaces with hyphens
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("&", "and");
            
        // Remove invalid characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        
        // Remove consecutive hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        
        // Trim hyphens from start and end
        slug = slug.Trim('-');
        
        return slug;
    }
    
    private async Task UpdateSiteMetadataAsync(BlogPost post)
    {
        var siteMetadataPath = _metadataHelper.GetSiteMetadataPath();
        var siteMetadata = await _metadataHelper.ReadMetadataAsync<SiteMetadata>(siteMetadataPath)
                           ?? new SiteMetadata();
            
        // Update tags
        foreach (var tag in post.Tags)
        {
            if (!siteMetadata.AllTags.Contains(tag))
            {
                siteMetadata.AllTags.Add(tag);
            }
        }
        
        // Update categories
        foreach (var category in post.Categories)
        {
            if (!siteMetadata.AllCategories.Contains(category))
            {
                siteMetadata.AllCategories.Add(category);
            }
        }
        
        siteMetadata.LastUpdated = DateTime.UtcNow;
        
        await _metadataHelper.WriteMetadataAsync(siteMetadataPath, siteMetadata);
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        try
        {
            // Search in published posts first
            var publishedDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Published");
            var post = await FindPostBySlugInDirectoryAsync(publishedDir, slug);
            
            if (post != null)
            {
                return post;
            }
            
            // If not found, search in drafts
            var draftsDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Drafts");
            return await FindPostBySlugInDirectoryAsync(draftsDir, slug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog post by slug: {Slug}", slug);
            return null;
        }
    }

    private async Task<BlogPost?> FindPostBySlugInDirectoryAsync(string directory, string slug)
    {
        if (!_fileSystem.DirectoryExists(directory))
        {
            return null;
        }
        
        // Get all subdirectories (each post has its own directory)
        var postDirectories = Directory.GetDirectories(directory);
        
        foreach (var postDir in postDirectories)
        {
            var metadataPath = Path.Combine(postDir, "meta.json");
            
            if (await _fileSystem.FileExistsAsync(metadataPath))
            {
                var metadata = await _metadataHelper.ReadMetadataAsync<BlogPostMetadata>(metadataPath);
                
                if (metadata?.Slug == slug)
                {
                    // Found the post
                    var contentPath = metadata.ContentFilePath;
                    var content = await _fileSystem.ReadTextAsync(contentPath);
                    
                    // Create the BlogPost from metadata and content
                    var post = new BlogPost
                    {
                        Id = metadata.Id,
                        Title = metadata.Title,
                        Slug = metadata.Slug,
                        Description = metadata.Description,
                        Content = content,
                        AuthorUsername = metadata.AuthorUsername,
                        AuthorDisplayName = metadata.AuthorDisplayName,
                        CreatedAt = metadata.CreatedAt,
                        PublishedAt = metadata.PublishedAt,
                        ModifiedAt = metadata.ModifiedAt,
                        Status = metadata.Status,
                        Tags = metadata.Tags,
                        Categories = metadata.Categories,
                        FeaturedImage = metadata.FeaturedImage
                    };
                    
                    return post;
                }
            }
        }
        
        return null;
    }
    
    public async Task<(IEnumerable<BlogPost> Posts, int TotalCount)> ListPostsAsync(
        int page = 1, 
        int pageSize = 10, 
        string? category = null,
        string? tag = null,
        string? author = null,
        bool includeDrafts = false,
        string? searchQuery = null,
        string sortBy = "PublishedAt",
        bool descending = true)
    {
        try
        {
            var posts = new List<BlogPost>();
            
            // Get all posts metadata first
            var publishedDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Published");
            var publishedPosts = await GetPostsFromDirectoryAsync(publishedDir);
            posts.AddRange(publishedPosts);
            
            // Include drafts if requested (typically for admin/author users)
            if (includeDrafts)
            {
                var draftsDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Drafts");
                var draftPosts = await GetPostsFromDirectoryAsync(draftsDir);
                posts.AddRange(draftPosts);
            }
            
            // Apply filters
            var filteredPosts = posts.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(category))
            {
                filteredPosts = filteredPosts.Where(p => p.Categories.Contains(category));
            }
            
            if (!string.IsNullOrWhiteSpace(tag))
            {
                filteredPosts = filteredPosts.Where(p => p.Tags.Contains(tag));
            }
            
            if (!string.IsNullOrWhiteSpace(author))
            {
                filteredPosts = filteredPosts.Where(p => p.AuthorUsername == author);
            }
            
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var query = searchQuery.ToLower();
                filteredPosts = filteredPosts.Where(p => 
                    p.Title.ToLower().Contains(query) || 
                    p.Description.ToLower().Contains(query) || 
                    p.Content.ToLower().Contains(query));
            }
            
            // Apply sorting
            filteredPosts = ApplySorting(filteredPosts, sortBy, descending);
            
            // Count total before pagination
            var totalCount = filteredPosts.Count();
            
            // Apply pagination
            var pagedPosts = filteredPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return (pagedPosts, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing blog posts");
            return (Enumerable.Empty<BlogPost>(), 0);
        }
    }

    private IQueryable<BlogPost> ApplySorting(IQueryable<BlogPost> posts, string sortBy, bool descending)
    {
        return sortBy.ToLower() switch
        {
            "title" => descending ? posts.OrderByDescending(p => p.Title) : posts.OrderBy(p => p.Title),
            "createdat" => descending ? posts.OrderByDescending(p => p.CreatedAt) : posts.OrderBy(p => p.CreatedAt),
            "publishedat" => descending ? posts.OrderByDescending(p => p.PublishedAt) : posts.OrderBy(p => p.PublishedAt),
            "modifiedat" => descending ? posts.OrderByDescending(p => p.ModifiedAt) : posts.OrderBy(p => p.ModifiedAt),
            _ => descending ? posts.OrderByDescending(p => p.PublishedAt) : posts.OrderBy(p => p.PublishedAt)
        };
    }

    private async Task<List<BlogPost>> GetPostsFromDirectoryAsync(string directory)
    {
        var posts = new List<BlogPost>();
        
        if (!_fileSystem.DirectoryExists(directory))
        {
            return posts;
        }
        
        // Get all post directories
        var postDirectories = Directory.GetDirectories(directory);
        
        foreach (var postDir in postDirectories)
        {
            var metadataPath = Path.Combine(postDir, "meta.json");
            
            if (await _fileSystem.FileExistsAsync(metadataPath))
            {
                var metadata = await _metadataHelper.ReadMetadataAsync<BlogPostMetadata>(metadataPath);
                
                if (metadata != null)
                {
                    var contentPath = metadata.ContentFilePath;
                    var content = await _fileSystem.ReadTextAsync(contentPath);
                    
                    posts.Add(new BlogPost
                    {
                        Id = metadata.Id,
                        Title = metadata.Title,
                        Slug = metadata.Slug,
                        Description = metadata.Description,
                        Content = content,
                        AuthorUsername = metadata.AuthorUsername,
                        AuthorDisplayName = metadata.AuthorDisplayName,
                        CreatedAt = metadata.CreatedAt,
                        PublishedAt = metadata.PublishedAt,
                        ModifiedAt = metadata.ModifiedAt,
                        Status = metadata.Status,
                        Tags = metadata.Tags,
                        Categories = metadata.Categories,
                        FeaturedImage = metadata.FeaturedImage
                    });
                }
            }
        }
        
        return posts;
    }
        
    public async Task<BlogPost?> UpdatePostAsync(string id, BlogPost updatedPost, string username)
    {
        try
        {
            // Find the original post first
            var originalPost = await GetPostByIdAsync(id);
            if (originalPost == null)
            {
                return null;
            }
            
            // Security check - only author or admin can update
            if (originalPost.AuthorUsername != username)
            {
                // In a real app, you'd check user roles here too
                _logger.LogWarning("User {Username} attempted to update post by {Author}", 
                    username, originalPost.AuthorUsername);
                return null;
            }
            
            // Update the post properties but preserve some metadata
            updatedPost.Id = originalPost.Id;
            updatedPost.AuthorUsername = originalPost.AuthorUsername;
            updatedPost.AuthorDisplayName = originalPost.AuthorDisplayName;
            updatedPost.CreatedAt = originalPost.CreatedAt;
            updatedPost.ModifiedAt = DateTime.UtcNow;
            
            // Check if the slug has changed and needs a redirect
            var oldSlug = originalPost.Slug;
            var newSlug = string.IsNullOrWhiteSpace(updatedPost.Slug) 
                ? _urlService.GenerateSlug(updatedPost.Title) 
                : updatedPost.Slug;
                
            if (_urlService.NeedsRedirect(oldSlug, newSlug))
            {
                // Add a redirect from old slug to new slug
                await _redirectService.AddRedirectAsync(
                    $"blog/{oldSlug}", 
                    $"blog/{newSlug}"
                );
                
                _logger.LogInformation("Added redirect from {OldSlug} to {NewSlug}", oldSlug, newSlug);
            }
            
            // Set the new slug
            updatedPost.Slug = newSlug;
            
            // If transitioning from Draft to Published, set PublishedAt
            if (originalPost.Status != PublishStatus.Published && 
                updatedPost.Status == PublishStatus.Published)
            {
                updatedPost.PublishedAt = DateTime.UtcNow;
            }
            else
            {
                updatedPost.PublishedAt = originalPost.PublishedAt;
            }
            
            // Determine if we need to move the post between directories
            var originalDirectory = GetPostDirectory(originalPost);
            var newDirectory = GetPostDirectory(updatedPost);
            bool needsMove = originalDirectory != newDirectory;
            
            if (needsMove)
            {
                // Create new directory
                await _fileSystem.EnsureDirectoryExistsAsync(newDirectory);
            }
            
            // Update content
            var contentPath = needsMove 
                ? Path.Combine(newDirectory, "content.md") 
                : Path.Combine(originalDirectory, "content.md");
                
            await _fileSystem.WriteTextAsync(contentPath, updatedPost.Content);
            
            // Create updated metadata
            var metadata = BlogPostMetadata.FromBlogPost(updatedPost);
            metadata.ContentFilePath = contentPath;
            metadata.DirectoryPath = needsMove ? newDirectory : originalDirectory;
            
            // Save metadata
            var metadataPath = needsMove 
                ? Path.Combine(newDirectory, "meta.json") 
                : Path.Combine(originalDirectory, "meta.json");
                
            await _metadataHelper.WriteMetadataAsync(metadataPath, metadata);
            
            // If we moved the post, delete the old directory
            if (needsMove && _fileSystem.DirectoryExists(originalDirectory))
            {
                Directory.Delete(originalDirectory, true);
            }
            
            // Update site metadata for new tags/categories
            await UpdateSiteMetadataAsync(updatedPost);
            
            return updatedPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog post: {Id}", id);
            return null;
        }
    }

    public async Task<BlogPost?> GetPostByIdAsync(string id)
    {
        try
        {
            // Search in published posts first
            var publishedDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Published");
            var post = await FindPostByIdInDirectoryAsync(publishedDir, id);
            
            if (post != null)
            {
                return post;
            }
            
            // If not found, search in drafts
            var draftsDir = Path.Combine(_fileSystem.GetPostsDirectory(), "Drafts");
            return await FindPostByIdInDirectoryAsync(draftsDir, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog post by ID: {Id}", id);
            return null;
        }
    }

    private async Task<BlogPost?> FindPostByIdInDirectoryAsync(string directory, string id)
    {
        if (!_fileSystem.DirectoryExists(directory))
        {
            return null;
        }
        
        // Check if the post directory exists directly (faster approach)
        var postDir = Path.Combine(directory, id);
        if (_fileSystem.DirectoryExists(postDir))
        {
            var metadataPath = Path.Combine(postDir, "meta.json");
            
            if (await _fileSystem.FileExistsAsync(metadataPath))
            {
                var metadata = await _metadataHelper.ReadMetadataAsync<BlogPostMetadata>(metadataPath);
                
                if (metadata != null)
                {
                    var contentPath = metadata.ContentFilePath;
                    var content = await _fileSystem.ReadTextAsync(contentPath);
                    
                    return new BlogPost
                    {
                        Id = metadata.Id,
                        Title = metadata.Title,
                        Slug = metadata.Slug,
                        Description = metadata.Description,
                        Content = content,
                        AuthorUsername = metadata.AuthorUsername,
                        AuthorDisplayName = metadata.AuthorDisplayName,
                        CreatedAt = metadata.CreatedAt,
                        PublishedAt = metadata.PublishedAt,
                        ModifiedAt = metadata.ModifiedAt,
                        Status = metadata.Status,
                        Tags = metadata.Tags,
                        Categories = metadata.Categories,
                        FeaturedImage = metadata.FeaturedImage
                    };
                }
            }
        }
        
        return null;
    }

    public async Task<bool> DeletePostAsync(string id, string username)
    {
        try
        {
            // Find the post
            var post = await GetPostByIdAsync(id);
            if (post == null)
            {
                return false;
            }
        
            // Security check - only author or admin can delete
            if (post.AuthorUsername != username)
            {
                // In a real app, you'd check user roles here too
                _logger.LogWarning("User {Username} attempted to delete post by {Author}", 
                    username, post.AuthorUsername);
                return false;
            }
        
            // Get the post directory
            var postDirectory = GetPostDirectory(post);
        
            // Delete the directory and all contents
            if (_fileSystem.DirectoryExists(postDirectory))
            {
                Directory.Delete(postDirectory, true);
                return true;
            }
        
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog post: {Id}", id);
            return false;
        }
    }
    
}