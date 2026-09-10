using Amazon.S3;
using Amazon.S3.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
var app = builder.Build();

var bucket = builder.Configuration["BUCKET"]
             ?? throw new Exception("BUCKET not set");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/bags/{tag}/image-url", (string tag, IAmazonS3 s3) =>
{
    var key = $"bags/{tag}/{Guid.NewGuid():N}.jpg";
    var url = s3.GetPreSignedURL(new GetPreSignedUrlRequest
    {
        BucketName  = bucket,
        Key         = key,
        Verb        = HttpVerb.PUT,
        Expires     = DateTime.UtcNow.AddMinutes(5),
        ContentType = "image/jpeg"
    });
    return Results.Ok(new { key, uploadUrl = url, expiresInSeconds = 300 });
});

app.Run();
