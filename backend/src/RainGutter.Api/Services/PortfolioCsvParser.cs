using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace RainGutter.Api.Services;

public record CsvEntry(string FbPostUrl, string? Title, DateTime? PostedDate, int? Reach);
public record CsvParseResult(List<CsvEntry> Entries, int Skipped, List<string> Errors);

public static class PortfolioCsvParser
{
    public static CsvParseResult Parse(Stream stream)
    {
        var entries = new List<CsvEntry>();
        var errors = new List<string>();
        int skipped = 0;

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        });

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            try
            {
                var postType = csv.GetField("ประเภทโพสต์") ?? "";
                var isShared = csv.GetField("เป็นโพสต์ที่แชร์หรือไม่") ?? "";
                var isCross = csv.GetField("เป็นโพสต์ข้าม") ?? "";

                // Real Meta Business Suite CSV uses "1"/"0" for boolean fields
                // ประเภทโพสต์ for photos is "รูปภาพ"; skip "วิดีโอ"
                if (postType == "วิดีโอ" || isShared == "1" || isCross == "1")
                {
                    skipped++;
                    continue;
                }

                var url = csv.GetField("ลิงก์ถาวร") ?? "";
                if (string.IsNullOrWhiteSpace(url)) { skipped++; continue; }

                // ชื่อ field is multi-line: "content\nmore content\n#hashtag"
                // Strip hashtag lines, join remaining, truncate to 100
                var rawTitle = csv.GetField("ชื่อ") ?? "";
                var titleLines = rawTitle.Split('\n')
                    .Where(l => !l.TrimStart().StartsWith('#'))
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0);
                var title = string.Join(" ", titleLines);
                if (title.Length > 100) title = title[..100];
                if (string.IsNullOrWhiteSpace(title)) title = null;

                DateTime? postedDate = null;
                var dateStr = csv.GetField("เวลาที่เผยแพร่") ?? "";
                if (DateTime.TryParseExact(dateStr, "MM/dd/yyyy HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    postedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                int? reach = null;
                var reachStr = csv.GetField("การเข้าถึง") ?? "";
                if (int.TryParse(reachStr.Replace(",", ""), out var r)) reach = r;

                entries.Add(new CsvEntry(url, string.IsNullOrWhiteSpace(title) ? null : title, postedDate, reach));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {csv.Context.Parser.Row}: {ex.Message}");
            }
        }

        return new CsvParseResult(entries, skipped, errors);
    }
}
