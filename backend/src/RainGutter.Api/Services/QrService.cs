using QRCoder;

namespace RainGutter.Api.Services;

public class QrService : IQrService
{
    public byte[] GenerateQrPng(string url)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var code = new PngByteQRCode(data);
        return code.GetGraphic(10);
    }
}
