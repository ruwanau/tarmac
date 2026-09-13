using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    _ => new AmazonSimpleNotificationServiceClient());

var app = builder.Build();

var bucket = builder.Configuration["BUCKET"]
             ?? throw new Exception("BUCKET not set");
var topic  = builder.Configuration["TOPIC_ARN"]
             ?? throw new Exception("TOPIC_ARN not set");

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

app.MapPost("/bags/{tag}/events", async (
    string tag, BagEvent ev, IAmazonSimpleNotificationService sns) =>
{
    var body = System.Text.Json.JsonSerializer.Serialize(new
    {
        tag,
        status   = ev.Status,
        location = ev.Location,
        at       = DateTime.UtcNow.ToString("o")
    });

    await sns.PublishAsync(topic, body);
    return Results.Accepted($"/bags/{tag}");
});

app.Run();

record BagEvent(string Status, string Location);
