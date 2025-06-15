namespace SkFileBlog.Shared.Models;

public class UserProfile
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public List<string> SocialLinks { get; set; } = new();
}