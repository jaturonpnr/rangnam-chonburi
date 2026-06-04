using System.IO.Compression;
using System.Text;
using RainGutter.Api.Services;

namespace RainGutter.Tests;

public class FbExportParserTests
{
    private static ZipArchive BuildZip(Dictionary<string, byte[]> entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, data) in entries)
            {
                var entry = zip.CreateEntry(name);
                using var es = entry.Open();
                es.Write(data);
            }
        }
        ms.Seek(0, SeekOrigin.Begin);
        return new ZipArchive(ms, ZipArchiveMode.Read);
    }

    [Fact]
    public void FixEncoding_ThaiMojibake_DecodesCorrectly()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var original = "สวัสดี";
        var fakeBytes = Encoding.UTF8.GetBytes(original);
        var mojibake = Encoding.GetEncoding("ISO-8859-1").GetString(fakeBytes);
        var fixed_ = FbExportParser.FixEncoding(mojibake);
        Assert.Equal(original, fixed_);
    }

    [Fact]
    public void FixEncoding_NullOrEmpty_ReturnsInput()
    {
        Assert.Null(FbExportParser.FixEncoding(null));
        Assert.Equal("", FbExportParser.FixEncoding(""));
    }

    [Fact]
    public void Parse_SinglePost_ReturnsPairedEntry()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var jsonContent = """
        [
          {
            "timestamp": 1700000000,
            "data": [{"post": "test post"}],
            "attachments": [{
              "data": [{
                "media": {
                  "uri": "media/photos/test.jpg",
                  "creation_timestamp": 1700000000,
                  "description": "caption text"
                }
              }]
            }]
          }
        ]
        """;
        using var zip = BuildZip(new Dictionary<string, byte[]>
        {
            ["posts/your_posts_1.json"] = Encoding.UTF8.GetBytes(jsonContent),
            ["media/photos/test.jpg"] = photoBytes
        });

        var result = FbExportParser.Parse(zip);
        Assert.Single(result.Paired);
        Assert.Equal("media/photos/test.jpg", result.Paired[0].ZipUri);
        Assert.Equal(1700000000L, result.Paired[0].CreationTimestamp);
        Assert.Equal("caption text", result.Paired[0].Description);
        Assert.Empty(result.UnpairedUris);
    }

    [Fact]
    public void Parse_UnpairedImage_AppearsInUnpaired()
    {
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var jsonContent = "[]";
        using var zip = BuildZip(new Dictionary<string, byte[]>
        {
            ["posts/your_posts_1.json"] = Encoding.UTF8.GetBytes(jsonContent),
            ["media/photos/orphan.jpg"] = photoBytes
        });

        var result = FbExportParser.Parse(zip);
        Assert.Empty(result.Paired);
        Assert.Contains(result.UnpairedUris, u => u.Contains("orphan.jpg"));
    }

    [Fact]
    public void Parse_VideoAttachment_IsSkipped()
    {
        var videoBytes = new byte[] { 0x00 };
        var jsonContent = """
        [
          {
            "timestamp": 1700000000,
            "attachments": [{
              "data": [{
                "media": {
                  "uri": "media/videos/clip.mp4",
                  "creation_timestamp": 1700000000
                }
              }]
            }]
          }
        ]
        """;
        using var zip = BuildZip(new Dictionary<string, byte[]>
        {
            ["posts/your_posts_1.json"] = Encoding.UTF8.GetBytes(jsonContent),
            ["media/videos/clip.mp4"] = videoBytes
        });

        var result = FbExportParser.Parse(zip);
        Assert.Empty(result.Paired);
    }

    [Fact]
    public void Parse_RootFolderPrefix_HandledCorrectly()
    {
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var jsonContent = """
        [
          {
            "timestamp": 1700000000,
            "attachments": [{
              "data": [{
                "media": {
                  "uri": "media/photos/test.jpg",
                  "creation_timestamp": 1700000000
                }
              }]
            }]
          }
        ]
        """;
        using var zip = BuildZip(new Dictionary<string, byte[]>
        {
            ["facebook-user123/posts/your_posts_1.json"] = Encoding.UTF8.GetBytes(jsonContent),
            ["facebook-user123/media/photos/test.jpg"] = photoBytes
        });

        var result = FbExportParser.Parse(zip);
        Assert.Single(result.Paired);
    }
}
