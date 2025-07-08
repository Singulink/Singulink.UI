namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root or nested route with all its parameters specified.
/// </summary>
public interface IConcreteRoute : IEquatable<IConcreteRoute>
{
    /// <summary>
    /// Gets the route that this concrete route is based on.
    /// </summary>
    public RouteBase Route { get; }
}
