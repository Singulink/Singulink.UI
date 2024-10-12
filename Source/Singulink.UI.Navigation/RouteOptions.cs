namespace Singulink.UI.Navigation;

/// <summary>
/// Represents options for a route.
/// </summary>
public sealed class RouteOptions(string? anchor = null)
{
    /// <summary>
    /// Gets the anchor for the route.
    /// </summary>
    public string? Anchor => anchor;
}
