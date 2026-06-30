using System.Collections.Generic;

using WinUIVLC.ViewModels;

namespace WinUIVLC.Helpers;

/// <summary>
/// Parses extended M3U / M3U8 playlists (the IPTV channel-list format) into playlist items.
/// </summary>
public static class M3uParser
{
    private static readonly HashSet<string> MediaSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "rtsp", "rtmp", "rtmps", "mms", "mmsh", "udp", "rtp", "ftp", "ftps", "smb", "hls", "file",
    };

    public static List<PlaylistItem> Parse(string content)
    {
        var items = new List<PlaylistItem>();
        if (string.IsNullOrEmpty(content))
        {
            return items;
        }

        string? title = null;
        string group = string.Empty;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var comma = FindSeparatorComma(line);
                title = comma >= 0 && comma < line.Length - 1 ? line[(comma + 1)..].Trim() : null;
                group = ExtractAttribute(line, "group-title");
            }
            else if (line.StartsWith("#"))
            {
                // Other directives (#EXTM3U, #EXTGRP, #EXTVLCOPT, ...) - ignored.
                continue;
            }
            else if (title != null || IsLikelyMediaUri(line))
            {
                // A URL line: trusted if it followed an #EXTINF, otherwise it must look like a real media URL.
                items.Add(new PlaylistItem
                {
                    Title = string.IsNullOrEmpty(title) ? line : title!,
                    Uri = line,
                    Group = group,
                });
                title = null;
                group = string.Empty;
            }

            // Anything else (e.g. stray HTML when the URL wasn't actually a playlist) is ignored.
        }

        return items;
    }

    private static bool IsLikelyMediaUri(string line)
    {
        return Uri.TryCreate(line, UriKind.Absolute, out var uri) && MediaSchemes.Contains(uri.Scheme);
    }

    // The channel title is the text after the first comma that is not inside a quoted attribute value.
    private static int FindSeparatorComma(string line)
    {
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                return i;
            }
        }

        return -1;
    }

    private static string ExtractAttribute(string line, string name)
    {
        var key = name + "=\"";
        var start = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        start += key.Length;
        var end = line.IndexOf('"', start);
        return end > start ? line[start..end] : string.Empty;
    }
}
