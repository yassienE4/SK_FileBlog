using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SkFileBlog.Features.Auth.Login;
using SkFileBlog.Features.Auth.Profile;
using SkFileBlog.Features.Auth.Register;
using SkFileBlog.Features.Media;
using SkFileBlog.Features.Media.Delete;
using SkFileBlog.Features.Media.List;
using SkFileBlog.Features.Media.Upload;
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

// Register media services
builder.Services.AddScoped<MediaService>();

// Register validation - single call registers all validators in assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

// Serve media files from the content directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), builder.Configuration["Blog:ContentRoot"] ?? "BlogContent")),
    RequestPath = "/media"
});

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

// Map authentication endpoints
app.MapLoginEndpoint();
app.MapRegisterEndpoint();
app.MapGetProfileEndpoint();

// Map media endpoints
app.MapUploadMediaEndpoint();
app.MapListMediaEndpoint();
app.MapDeleteMediaEndpoint();

// Public endpoints
app.MapGet("/", () => "Welcome to SkFileBlog API")
   .WithName("GetWelcomeMessage");

app.Run();