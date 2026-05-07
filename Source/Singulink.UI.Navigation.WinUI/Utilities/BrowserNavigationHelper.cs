#if __WASM__

using System.Runtime.InteropServices.JavaScript;

namespace Singulink.UI.Navigation.WinUI.Utilities;

internal static partial class BrowserNavigationHelper
{
    /// <summary>
    /// Pushes a new browser history entry using <c>history.pushState</c>. Must be called synchronously while user activation is still valid (i.e. directly
    /// from a user-initiated event handler) so that the browser does not flag the entry as skip-on-back.
    /// </summary>
    [JSImport("globalThis.history.pushState")]
    internal static partial void PushState(double state, string title, string url);

    /// <summary>
    /// Replaces the current browser history entry using <c>history.replaceState</c>.
    /// </summary>
    [JSImport("globalThis.history.replaceState")]
    internal static partial void ReplaceState(double state, string title, string url);

    /// <summary>
    /// Calls <c>history.back()</c> to navigate back one entry in the browser history.
    /// </summary>
    [JSImport("globalThis.history.back")]
    internal static partial void Back();

    /// <summary>
    /// Calls <c>history.forward()</c> to navigate forward one entry in the browser history.
    /// </summary>
    [JSImport("globalThis.history.forward")]
    internal static partial void Forward();

    /// <summary>
    /// Adds an event listener to <c>globalThis</c> (the browser <c>window</c>).
    /// </summary>
    [JSImport("globalThis.addEventListener")]
    internal static partial void AddEventListener(string type, [JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> listener);

    /// <summary>
    /// Gets the full current browser URL (i.e. <c>location.href</c>).
    /// </summary>
    [JSImport("globalThis.location.href.toString")]
    internal static partial string GetCurrentUrl();
}

#endif
