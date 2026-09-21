namespace Singulink.UI.Navigation;

/// <summary>
/// Converts between in-app routes and browser paths for an application hosted under a site sub-folder (the WebAssembly base path).
/// </summary>
internal static class BrowserRoutePaths
{
    /// <summary>
    /// Resolves the site path the application is hosted under, always starting and ending with a slash.
    /// </summary>
    /// <param name="configuredBasePath">The configured base path (the <c>UNO_BOOTSTRAP_WEBAPP_BASE_PATH</c> environment variable), or <see langword="null"/>.</param>
    /// <param name="initialDocumentPath">The absolute path of the document the application was loaded from, used when the configured value is relative or
    /// absent.</param>
    public static string ResolveBasePath(string? configuredBasePath, string initialDocumentPath)
    {
        string configured = configuredBasePath?.Trim() ?? string.Empty;

        if (configured.Contains("://", StringComparison.Ordinal) && Uri.TryCreate(configured, UriKind.Absolute, out var uri))
            configured = uri.AbsolutePath;

        if (configured.StartsWith('/'))
            return configured.EndsWith('/') ? configured : configured + "/";

        // A relative or missing base path means the application lives wherever its document was served from.
        int lastSlash = initialDocumentPath.LastIndexOf('/');
        return lastSlash < 0 ? "/" : initialDocumentPath[..(lastSlash + 1)];
    }

    /// <summary>
    /// Builds the browser URL (path, query and fragment) for an in-app route.
    /// </summary>
    public static string ToBrowserUrl(string basePath, string route) => basePath + route.TrimStart('/');

    /// <summary>
    /// Extracts the in-app route (starting with a slash) from a browser path, query and fragment. A path outside the base path is returned unchanged.
    /// </summary>
    public static string ToRoute(string basePath, string browserPath)
    {
        if (basePath.Length <= 1)
            return browserPath;

        if (browserPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return "/" + browserPath[basePath.Length..];

        // The base folder itself, without its trailing slash and possibly followed by a query or fragment.
        int folderLength = basePath.Length - 1;

        if (browserPath.StartsWith(basePath.AsSpan(0, folderLength), StringComparison.OrdinalIgnoreCase) &&
            (browserPath.Length == folderLength || browserPath[folderLength] is '?' or '#'))
        {
            return "/" + browserPath[folderLength..];
        }

        return browserPath;
    }
}
