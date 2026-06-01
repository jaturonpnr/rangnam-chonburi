using Amazon.S3;
using Amazon.S3.Model;

namespace RainGutter.Api.Services;

public class StorageService : IStorageService
{
    private AmazonS3Client CreateClient()
    {
        var endpoint = Env("STORAGE_ENDPOINT");
        var accessKey = Env("STORAGE_ACCESS_KEY");
        var secret = Env("STORAGE_SECRET");
        var config = new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true };
        return new AmazonS3Client(accessKey, secret, config);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType)
    {
        var bucket = Env("STORAGE_BUCKET");
        var publicBase = Env("STORAGE_PUBLIC_BASE_URL");
        using var client = CreateClient();
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = fileName,
            InputStream = content,
            ContentType = contentType
        };
        await client.PutObjectAsync(request);
        return $"{publicBase.TrimEnd('/')}/{fileName}";
    }

    public async Task DeleteAsync(string fileName)
    {
        var bucket = Env("STORAGE_BUCKET");
        using var client = CreateClient();
        await client.DeleteObjectAsync(bucket, fileName);
    }

    private static string Env(string key) =>
        Environment.GetEnvironmentVariable(key)
        ?? throw new InvalidOperationException($"{key} env var is not set");
}
