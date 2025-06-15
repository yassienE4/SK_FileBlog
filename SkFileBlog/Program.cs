using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkFileBlog.Features.Posts;
using SkFileBlog.Features.Posts.Create;
using SkFileBlog.Features.Posts.Get;
using SkFileBlog.Features.Posts.List;
using SkFileBlog.Features.Posts.Update;
using SkFileBlog.Features.Posts.Delete;
using SkFileBlog.Infrastructure.Authentication;
using SkFileBlog.Infrastructure.FileSystem;
using SkFileBlog.Infrastructure.Markdown;
using SkFileBlog.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and endpoints API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register core services
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddSingleton<IMarkdownProcessor, MarkdownProcessor>();
builder.Services.AddScoped<BlogPostProcessor>();

// Register blog post services
builder.Services.AddScoped<MetadataHelper>();
builder.Services.AddScoped<PostService>();

// Register validation
builder.Services.AddValidatorsFromAssemblyContaining<CreatePostValidator>();

// Register authentication services
builder.Services.AddSingleton<JwtProvider>();
builder.Services.AddScoped<UserService>();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
    };
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AuthorOrAdmin", policy => 
        policy.RequireRole("Author", "Admin"));
    options.AddPolicy("EditorOrAdmin", policy => 
        policy.RequireRole("Editor", "Admin"));
});

var app = builder.Build();

// Initialize directory structure and admin user
using (var scope = app.Services.CreateScope())
{
    var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystemService>();
    await DirectoryStructureInitializer.InitializeAsync(fileSystem);
    
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    await UserSetupUtility.EnsureAdminUserExists(userService, builder.Configuration);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map blog post endpoints
app.MapCreatePostEndpoint();
app.MapGetPostEndpoint();
app.MapListPostsEndpoint();
app.MapUpdatePostEndpoint();
app.MapDeletePostEndpoint();

// Auth endpoints
app.MapPost("/api/auth/login", async ([FromBody] LoginRequest request, 
                                   [FromServices] UserService userService) =>
{
    var token = await userService.AuthenticateAsync(request.Username, request.Password);
    
    if (token == null)
    {
        return Results.Unauthorized();
    }
    
    return Results.Ok(new { Token = token });
})
.WithName("Login")
.Produces<object>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized);

// Public endpoints
app.MapGet("/", () => "Welcome to SkFileBlog API")
   .WithName("GetWelcomeMessage");

app.Run();

// Define login request class for the endpoint
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}