using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SkFileBlog.Infrastructure.Authentication;
using SkFileBlog.Infrastructure.FileSystem;
using SkFileBlog.Infrastructure.Markdown;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and endpoints API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register core services
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddSingleton<IMarkdownProcessor, MarkdownProcessor>();
builder.Services.AddScoped<BlogPostProcessor>();

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

// Example minimal API endpoints
app.MapGet("/", () => "Welcome to SkFileBlog API")
   .WithName("GetWelcomeMessage");

// Public endpoints - no authentication required
app.MapGet("/posts", () => "List of public blog posts")
   .WithName("GetPosts");

app.MapGet("/posts/{slug}", (string slug) => $"Blog post: {slug}")
   .WithName("GetPostBySlug");

// Protected endpoints - authentication required
app.MapGet("/protected", () => "This endpoint requires authentication")
   .RequireAuthorization();

// Admin-only endpoints
app.MapGet("/admin", () => "This endpoint is for admins only")
   .RequireAuthorization("AdminOnly");

app.MapGet("/author", () => "This endpoint is for authors and admins")
   .RequireAuthorization("AuthorOrAdmin");

// Auth endpoints
app.MapPost("/login", () => "Login endpoint - to be implemented")
   .WithName("Login");

app.MapPost("/register", () => "Register endpoint - to be implemented")
   .WithName("Register");

app.Run();