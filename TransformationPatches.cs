using System;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.NotificationCenter.Models;

namespace Jellyfin.Plugin.NotificationCenter;

public static class TransformationPatches
{
    public static Func<string> GetVersion { get; set; } = () => "unknown";

    public static string IndexHtml(PatchRequestPayload payload)
    {
        if (string.IsNullOrEmpty(payload.Contents))
            return string.Empty;

        var html = payload.Contents;

        if (!html.Contains("</body>"))
            return html;

        html = Regex.Replace(html, @"<script plugin=""NotificationCenter""[^>]*></script>\n?", "");

        var version = GetVersion();
        var script = $"<script plugin=\"NotificationCenter\" version=\"{version}\" src=\"../NotificationCenter/client.js\" defer></script>";

        html = html.Replace("</body>", $"{script}\n</body>");

        return html;
    }
}
