using Microsoft.AspNetCore.Mvc;

namespace SkFileBlog.Features.Media.Upload;

public static class UploadMediaEndpoint
{
    public static void MapUploadMediaEndpoint(this WebApplication app)
    {
        app.MapPost("/api/media/upload", async (HttpContext context,
                                          [FromServices] MediaService mediaService,
                                          [FromServices] ILogger<Program> logger) =>
        {
            // Get username from authenticated user
            var username = context.User.Identity?.Name ?? "anonymous";
            
            // Check for Anonymous uploads
            if (username == "anonymous")
            {
                return Results.Unauthorized();
            }
            
            // Get the form file
            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            
            if (file == null)
            {
                return Results.BadRequest("No file was uploaded");
            }
            
            // Check file size
            var maxFileSizeMb = app.Configuration.GetValue<int>("Media:MaxFileSizeMB", 10);
            var maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;
            
            if (file.Length > maxFileSizeBytes)
            {
                return Results.BadRequest($"File size exceeds the maximum allowed size of {maxFileSizeMb}MB");
            }
            
            // Check if resize is requested
            var resizeRequested = form.TryGetValue("resize", out var resizeValue) && 
                                 bool.TryParse(resizeValue, out var resize) && 
                                 resize;
            
            try
            {
                var (fileName, filePath, url, fileSize, contentType) = 
                    await mediaService.UploadFileAsync(file, username, resizeRequested);
                
                var response = new MediaUploadResponse
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Url = url,
                    FileSize = fileSize,
                    ContentType = contentType,
                    UploadedAt = DateTime.UtcNow
                };
                
                return Results.Created(url, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                return Results.Problem("Error uploading file", statusCode: 500);
            }
        })
        .RequireAuthorization()
        .WithName("UploadMedia")
        .WithDisplayName("Upload media file")
        .Produces<MediaUploadResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}