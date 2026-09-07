using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace IconPackBuilder.VsCode;

/// <summary>
/// Bindings to the <c>window.ipbHost</c> bridge installed by the VS Code extension's webview page (see IconPackBuilder.VSCode/DEVELOPMENT.md).
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class VsCodeHost
{
    [JSImport("globalThis.ipbHost.whenReady")]
    internal static partial Task WhenReady();

    [JSImport("globalThis.ipbHost.getDocumentText")]
    internal static partial string GetDocumentText();

    [JSImport("globalThis.ipbHost.setDocumentText")]
    internal static partial void SetDocumentText(string text);

    [JSImport("globalThis.ipbHost.onDocumentChanged")]
    internal static partial void OnDocumentChanged([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

    [JSImport("globalThis.ipbHost.exportProject")]
    internal static partial Task<string> ExportProject(string requestJson);

    [JSImport("globalThis.ipbHost.showMessage")]
    internal static partial Task<int> ShowMessage(string message, string title, string buttonsJson);

    [JSImport("globalThis.ipbHost.getDocumentFileName")]
    internal static partial string GetDocumentFileName();

    [JSImport("globalThis.ipbHost.getDocumentDir")]
    internal static partial string GetDocumentDir();

    /// <summary>
    /// Gets the VS Code color theme kind: <c>light</c>, <c>dark</c>, <c>highContrast</c> or <c>highContrastLight</c>.
    /// </summary>
    [JSImport("globalThis.ipbHost.getThemeKind")]
    internal static partial string GetThemeKind();

    [JSImport("globalThis.ipbHost.onThemeChanged")]
    internal static partial void OnThemeChanged([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

    [JSImport("globalThis.ipbHost.hideLoadingIndicator")]
    internal static partial void HideLoadingIndicator();
}
