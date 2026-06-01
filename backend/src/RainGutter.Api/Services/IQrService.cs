namespace RainGutter.Api.Services;

public interface IQrService
{
    byte[] GenerateQrPng(string url);
}
