using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace RainGutter.Api.Services;

public record FbMediaEntry(
    string ZipUri,
    long CreationTimestamp,
    string? Description,
    string? PostText);

public record FbParseResult(
    List<FbMediaEntry> Paired,
    List<string> UnpairedUris);

public static class FbExportParser
{
    public static FbParseResult Parse(ZipArchive zip)
    {
        var imageExts = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
        var imageMap = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
            if (!imageExts.Contains(ext)) continue;
            var normalized = entry.FullName.Replace('\\', '/');
            imageMap[normalized] = entry;
            var slash = normalized.IndexOf('/');
            if (slash >= 0)
                imageMap.TryAdd(normalized[(slash + 1)..], entry);
        }

        var jsonEntries = zip.Entries
            .Where(e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                     && e.FullName.Replace('\\', '/').Contains("posts/"))
            .ToList();

        var paired = new List<FbMediaEntry>();
        var pairedUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var jsonEntry in jsonEntries)
        {
            using var stream = jsonEntry.Open();
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            JsonElement postsArray;
            if (root.ValueKind == JsonValueKind.Array)
                postsArray = root;
            else if (root.TryGetProperty("posts", out var p))
                postsArray = p;
            else
                continue;

            foreach (var post in postsArray.EnumerateArray())
            {
                var postTimestamp = post.TryGetProperty("timestamp", out var ts)
                    ? ts.GetInt64() : 0L;

                string? postText = null;
                if (post.TryGetProperty("data", out var dataArr))
                {
                    foreach (var d in dataArr.EnumerateArray())
                    {
                        if (d.TryGetProperty("post", out var pt))
                        {
                            postText = FixEncoding(pt.GetString());
                            break;
                        }
                    }
                }

                if (!post.TryGetProperty("attachments", out var attachments)) continue;

                foreach (var attachment in attachments.EnumerateArray())
                {
                    if (!attachment.TryGetProperty("data", out var attData)) continue;
                    foreach (var item in attData.EnumerateArray())
                    {
                        if (!item.TryGetProperty("media", out var media)) continue;

                        var uri = media.TryGetProperty("uri", out var uriProp)
                            ? uriProp.GetString() ?? "" : "";
                        var mediaTs = media.TryGetProperty("creation_timestamp", out var mts)
                            ? mts.GetInt64() : postTimestamp;
                        var description = media.TryGetProperty("description", out var desc)
                            ? FixEncoding(desc.GetString()) : null;
                        var title = media.TryGetProperty("title", out var ttl)
                            ? FixEncoding(ttl.GetString()) : null;

                        var ext = Path.GetExtension(uri).ToLowerInvariant();
                        if (ext is ".mp4" or ".mov" or ".avi") continue;

                        var caption = description ?? title ?? postText;
                        var normalizedUri = uri.Replace('\\', '/');
                        var slashIdx = normalizedUri.IndexOf('/');
                        var shortUri = slashIdx >= 0 ? normalizedUri[(slashIdx + 1)..] : normalizedUri;

                        if (!imageMap.ContainsKey(normalizedUri) && !imageMap.ContainsKey(shortUri))
                            continue;

                        pairedUris.Add(normalizedUri);
                        pairedUris.Add(shortUri);

                        paired.Add(new FbMediaEntry(
                            ZipUri: normalizedUri,
                            CreationTimestamp: mediaTs,
                            Description: caption,
                            PostText: postText));
                    }
                }
            }
        }

        var unpairedUris = imageMap.Keys
            .Where(k => !pairedUris.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FbParseResult(paired, unpairedUris);
    }

    public static ZipArchiveEntry? FindEntry(ZipArchive zip, string uri)
    {
        var normalized = uri.Replace('\\', '/');
        foreach (var entry in zip.Entries)
        {
            var entryNorm = entry.FullName.Replace('\\', '/');
            if (entryNorm.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return entry;
            var slash = entryNorm.IndexOf('/');
            if (slash >= 0 && entryNorm[(slash + 1)..].Equals(normalized, StringComparison.OrdinalIgnoreCase))
                return entry;
        }
        return null;
    }

    public static string? FixEncoding(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        try
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return text;
        }
    }
}
