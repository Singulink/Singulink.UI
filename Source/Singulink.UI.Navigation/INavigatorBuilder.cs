namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator builder that can be used to configure a navigator.
/// </summary>
public interface INavigatorBuilder
{
    /// <summary>
    /// Gets or sets the max depth of the back stack. Default is 15.
    /// </summary>
    public int MaxBackStackDepth { get; set; }

    /// <summary>
    /// Adds a route to the navigator.
    /// </summary>
    public void AddRoute(RouteBase route);
}
