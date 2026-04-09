#if __WASM__

using System.Runtime.InteropServices.JavaScript;

namespace Singulink.UI.Navigation.WinUI.Utilities;

internal static partial class BrowserNavigationHelper
{
    /// <summary>
    /// Replaces the current browser history entry with the specified URL using <c>history.replaceState</c>.
    /// </summary>
    [JSImport("globalThis.history.replaceState")]
    internal static partial void ReplaceState([JSMarshalAs<JSType.Any>] object? state, string title, string url);

    /// <summary>
    /// Gets the full current browser URL (i.e. <c>location.href</c>).
    /// </summary>
    [JSImport("globalThis.location.href.toString")]
    internal static partial string GetCurrentUrl();
}

#endif
