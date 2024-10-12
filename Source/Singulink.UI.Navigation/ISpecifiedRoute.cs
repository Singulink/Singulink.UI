namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root or nested route with all its parameters specified.
/// </summary>
public interface ISpecifiedRoute : IEquatable<ISpecifiedRoute>
{
    /// <summary>
    /// Gets the route that this specified route is based on.
    /// </summary>
    public RouteBase Route { get; }
}
