using SkFileBlog.Infrastructure.FileSystem;
using SkFileBlog.Infrastructure.Markdown;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddSingleton<IMarkdownProcessor, MarkdownProcessor>();
builder.Services.AddScoped<BlogPostProcessor>();

using (var scope = app.Services.CreateScope())
{
    var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystemService>();
    await DirectoryStructureInitializer.InitializeAsync(fileSystem);
}
app.MapGet("/", () => "Hello World!");

app.Run();