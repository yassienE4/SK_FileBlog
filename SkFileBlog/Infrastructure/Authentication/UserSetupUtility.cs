namespace SkFileBlog.Infrastructure.Authentication;

public class UserSetupUtility
{
    public static async Task EnsureAdminUserExists(UserService userService, IConfiguration configuration)
    {
        // Check if admin exists
        var adminUsername = configuration["Auth:AdminUsername"] ?? "admin";
        var admin = await userService.GetUserAsync(adminUsername);

        // If admin doesn't exist, create one with default credentials
        if (admin == null)
        {
            var defaultPassword = configuration["Auth:DefaultAdminPassword"] ?? "Admin@123";
            var adminEmail = configuration["Auth:AdminEmail"] ?? "admin@example.com";
            
            await userService.CreateUserAsync(
                adminUsername, 
                adminEmail, 
                "Administrator", 
                defaultPassword,
                new List<string> { "Admin" }
            );
            
            
        }
    }
}