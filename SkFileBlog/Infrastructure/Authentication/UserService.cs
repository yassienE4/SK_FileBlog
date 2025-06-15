using System.Text.Json;
using SkFileBlog.Infrastructure.Authentication.Models;
using SkFileBlog.Infrastructure.FileSystem;

namespace SkFileBlog.Infrastructure.Authentication;

public class UserService
{
    private readonly IFileSystemService _fileSystem;
    private readonly JwtProvider _jwtProvider;
    private readonly ILogger<UserService> _logger;

    public UserService(IFileSystemService fileSystem, JwtProvider jwtProvider, ILogger<UserService> logger)
    {
        _fileSystem = fileSystem;
        _jwtProvider = jwtProvider;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(string username)
    {
        var userPath = GetUserProfilePath(username);
        if (!await _fileSystem.FileExistsAsync(userPath))
        {
            return null;
        }

        try
        {
            var userJson = await _fileSystem.ReadTextAsync(userPath);
            return JsonSerializer.Deserialize<User>(userJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading user profile for {Username}", username);
            return null;
        }
    }

    public async Task<bool> SaveUserAsync(User user)
    {
        try
        {
            var userPath = GetUserProfilePath(user.Username);
            var userJson = JsonSerializer.Serialize(user, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await _fileSystem.WriteTextAsync(userPath, userJson);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user profile for {Username}", user.Username);
            return false;
        }
    }

    public async Task<bool> CreateUserAsync(string username, string email, string displayName, string password, List<string> roles)
    {
        // Check if user already exists
        if (await GetUserAsync(username) != null)
        {
            return false;
        }

        // Create new user with hashed password
        var passwordHash = _jwtProvider.GeneratePasswordHash(password, out var salt);
        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Salt = salt,
            Roles = roles,
            CreatedAt = DateTime.UtcNow
        };

        return await SaveUserAsync(user);
    }

    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        var user = await GetUserAsync(username);
        if (user == null)
        {
            return null;
        }

        if (!_jwtProvider.VerifyPassword(password, user.PasswordHash, user.Salt))
        {
            return null;
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await SaveUserAsync(user);

        // Generate JWT token
        return _jwtProvider.GenerateToken(user);
    }

    public async Task<List<string>> ListUsersAsync()
    {
        var usersDirectory = _fileSystem.GetUsersDirectory();
        var userFiles = await _fileSystem.ListFilesInDirectoryAsync(usersDirectory);
        
        return userFiles
            .Where(path => Path.GetFileName(path) == "profile.json")
            .Select(path => Path.GetFileName(Path.GetDirectoryName(path)))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private string GetUserProfilePath(string username)
    {
        var userDirectory = Path.Combine(_fileSystem.GetUsersDirectory(), username);
        return Path.Combine(userDirectory, "profile.json");
    }
}