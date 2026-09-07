using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace IconPackBuilder.Browser;

/// <summary>
/// Bindings to the <c>globalThis.ipbBrowser</c> helpers defined in <c>Platforms/WebAssembly/WasmScripts/ipbBrowserHost.js</c>, which the
/// bootstrapper loads before the app starts.
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class BrowserHost
{
    /// <summary>
    /// Returns whether the page is the VS Code extension's webview (which installs its own <c>ipbHost</c> bridge) rather than a plain browser.
    /// </summary>
    [JSImport("globalThis.ipbBrowser.isVsCode")]
    internal static partial bool IsVsCode();

    [JSImport("globalThis.ipbBrowser.setTitle")]
    internal static partial void SetTitle(string title);

    /// <summary>
    /// Gets the browser's preferred color scheme: <c>light</c> or <c>dark</c>.
    /// </summary>
    [JSImport("globalThis.ipbBrowser.getThemeKind")]
    internal static partial string GetThemeKind();

    [JSImport("globalThis.ipbBrowser.onThemeChanged")]
    internal static partial void OnThemeChanged([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

    [JSImport("globalThis.ipbBrowser.storageGet")]
    internal static partial string? StorageGet(string key);

    [JSImport("globalThis.ipbBrowser.storageSet")]
    internal static partial void StorageSet(string key, string value);

    [JSImport("globalThis.ipbBrowser.storageRemove")]
    internal static partial void StorageRemove(string key);

    /// <summary>
    /// Gets the local storage keys starting with the given prefix as a JSON string array.
    /// </summary>
    [JSImport("globalThis.ipbBrowser.storageKeys")]
    internal static partial string StorageKeys(string prefix);

    /// <summary>
    /// Shows the browser's file picker and returns a JSON object with the chosen file's <c>name</c> and <c>text</c>, or <see langword="null"/>
    /// if the user cancelled.
    /// </summary>
    [JSImport("globalThis.ipbBrowser.pickFile")]
    internal static partial Task<string?> PickFile(string accept);

    [JSImport("globalThis.ipbBrowser.downloadFile")]
    internal static partial void DownloadFile(string fileName, string contentBase64, string mimeType);

    /// <summary>
    /// Subsets the font (base64) to the given code points (JSON array) with fontTools running in Pyodide, which is downloaded on first use.
    /// Returns the subset font as base64.
    /// </summary>
    [JSImport("globalThis.ipbBrowser.subsetFont")]
    internal static partial Task<string> SubsetFont(string fontBase64, string codePointsJson);
}
