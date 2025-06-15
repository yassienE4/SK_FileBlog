using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SkFileBlog.Infrastructure.Authentication.Models;

namespace SkFileBlog.Infrastructure.Authentication;

public class JwtProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtProvider> _logger;

    public JwtProvider(IConfiguration configuration, ILogger<JwtProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:DurationInHours"] ?? "1")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            return new ClaimsPrincipal(new ClaimsIdentity(
                tokenHandler.ReadJwtToken(token).Claims, "jwt"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    public string GeneratePasswordHash(string password, out string salt)
    {
        // Generate a random salt
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        salt = Convert.ToBase64String(saltBytes);

        // Hash the password with the salt
        using var sha256 = SHA256.Create();
        var passwordWithSalt = password + salt;
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        // Hash the provided password with the stored salt
        using var sha256 = SHA256.Create();
        var passwordWithSalt = password + storedSalt;
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
        var computedHash = Convert.ToBase64String(hashBytes);

        // Compare the computed hash with the stored hash
        return computedHash == storedHash;
    }
}