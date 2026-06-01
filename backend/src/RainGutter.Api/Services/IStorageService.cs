namespace RainGutter.Api.Services;

public interface IStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, string contentType);
    Task DeleteAsync(string fileName);
}
