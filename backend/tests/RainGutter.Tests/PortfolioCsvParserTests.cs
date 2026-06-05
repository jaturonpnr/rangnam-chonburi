using RainGutter.Api.Services;
using System.Text;
using Xunit;

namespace RainGutter.Tests;

public class PortfolioCsvParserTests
{
    private static Stream ToStream(string csv)
    {
        // Add UTF-8 BOM
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return new MemoryStream(bytes);
    }

    [Fact]
    public void Parse_NormalPhotoPost_ReturnsEntry()
    {
        // Use real column structure from Meta Business Suite export
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "123,ติดตั้งรางน้ำสแตนเลส,06/01/2026 09:30,https://www.facebook.com/page/posts/123,0,0,รูปภาพ,500";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Single(result.Entries);
        Assert.Equal("https://www.facebook.com/page/posts/123", result.Entries[0].FbPostUrl);
        Assert.Equal(500, result.Entries[0].Reach);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public void Parse_VideoPost_Skipped()
    {
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "456,วิดีโอรางน้ำ,06/02/2026 10:00,https://www.facebook.com/page/posts/456,0,0,วิดีโอ,200";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Empty(result.Entries);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public void Parse_SharedPost_Skipped()
    {
        // เป็นโพสต์ที่แชร์หรือไม่ uses "1"/"0" in real Meta Business Suite CSV
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "789,โพสต์แชร์,06/03/2026 11:00,https://www.facebook.com/page/posts/789,0,1,รูปภาพ,100";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Empty(result.Entries);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public void Parse_CrossPost_Skipped()
    {
        // เป็นโพสต์ข้าม uses "1"/"0" in real Meta Business Suite CSV
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "999,ข้ามโพสต์,06/04/2026 12:00,https://www.facebook.com/page/posts/999,1,0,รูปภาพ,50";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Empty(result.Entries);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public void Parse_DateParsed_Correctly()
    {
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "100,โพสต์,12/31/2025 23:59,https://www.facebook.com/page/posts/100,0,0,รูปภาพ,300";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Single(result.Entries);
        Assert.Equal(new DateTime(2025, 12, 31, 23, 59, 0), result.Entries[0].PostedDate);
    }

    [Fact]
    public void Parse_HashtagLinesStrippedFromTitle()
    {
        // Real data: ชื่อ field contains "content\nmore content\n#hashtag"
        // Title should strip hashtag lines and truncate to 100
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  "\"200\",\"รางน้ำแสตนเลส\nบ้านไม้เก่าปรับปรุงใหม่ค่ะ\n#รางน้ำฝนชลบุรี\",01/01/2026 08:00,https://www.facebook.com/page/posts/200,0,0,รูปภาพ,400";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.DoesNotContain("#", result.Entries[0].Title ?? "");
        Assert.True(result.Entries[0].Title?.Length <= 100);
    }

    [Fact]
    public void Parse_TitleTruncatedAt100Chars()
    {
        var longTitle = new string('ก', 150);
        var csv = "\"ID โพสต์\",ชื่อ,เวลาที่เผยแพร่,ลิงก์ถาวร,เป็นโพสต์ข้าม,เป็นโพสต์ที่แชร์หรือไม่,ประเภทโพสต์,การเข้าถึง\n" +
                  $"\"300\",\"{longTitle}\",01/15/2026 10:00,https://www.facebook.com/page/posts/300,0,0,รูปภาพ,100";
        var result = PortfolioCsvParser.Parse(ToStream(csv));
        Assert.Single(result.Entries);
        Assert.Equal(100, result.Entries[0].Title?.Length);
    }
}
