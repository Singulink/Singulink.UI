using System.Diagnostics;

namespace Singulink.UI.Navigation;

/// <summary>
/// Keeps the views and view models of a route materialized until the pin is disposed. Created with <see cref="INavigator.PinCurrentRoute"/>.
/// </summary>
/// <remarks>
/// <para>
/// While a pin is active, the pinned route's leaf view and view model are retained regardless of the navigator's caching settings and regardless of whether
/// the route remains in the navigation history, together with each ancestor up to the first one that is not pinnable (see <see
/// cref="IRoutedViewModelBase.CanBePinned"/>). Navigating to the route again (see <see cref="INavigator.NavigateAsync(NavigatorRoute)"/>) reuses the
/// retained instances with their state intact.</para>
/// <para>
/// Ancestors that are not pinned follow the normal caching rules. If one of them is evicted and the pinned view models depended on services it provided, the
/// pinned view models are evicted with it and the pin stops holding anything (see <see cref="IsPinned"/>).</para>
/// <para>
/// Disposing the pin releases the retained instances on the next navigation unless they are still cached or active by then. Pins are released automatically
/// when the navigator shuts down. The navigator does not keep pins alive, so a pin that is dropped without being disposed is released once it has been
/// garbage collected, but pins should always be disposed deterministically and debug builds report ones that were not.</para>
/// </remarks>
public sealed partial class RoutePin : IDisposable
{
    private NavigatorCore? _navigator;

    internal RoutePin(NavigatorCore navigator, NavigatorRoute route)
    {
        _navigator = navigator;
        Route = route;
    }

#if DEBUG
    /// <summary>
    /// Reports a pin that was dropped without being disposed. Only present in debug builds.
    /// </summary>
    ~RoutePin()
    {
        if (_navigator is null)
            return;

        string message = $"A route pin for route '{Route}' was dropped without being disposed. Its retained views and view models are released on the " +
            "next navigation, but route pins should be disposed deterministically.";

        // Failing an assertion off the debugger terminates the process, which is not appropriate from a finalizer.

        if (Debugger.IsAttached)
            Debug.Fail(message);
        else
            Debug.WriteLine(message);
    }
#endif

    /// <summary>
    /// Gets the pinned route.
    /// </summary>
    public NavigatorRoute Route { get; }

    /// <summary>
    /// Gets a value indicating whether the pin is still holding the route's views and view models. Returns <see langword="false"/> once the pin has been
    /// disposed, the retained view models have been evicted along with an ancestor they depended on, or the navigator has shut down.
    /// </summary>
    public bool IsPinned => _navigator is not null;

    /// <summary>
    /// Releases the pin. The route's views and view models are released on the next navigation unless they are still cached or active by then.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_navigator is { } navigator)
        {
            _navigator = null;
            navigator.ReleasePin(this);
        }
    }

    /// <summary>
    /// Marks the pin as no longer holding anything without notifying the navigator, for use when the navigator itself releases the pin. The finalizer (if
    /// present) then has nothing to report.
    /// </summary>
    internal void Invalidate() => _navigator = null;
}
