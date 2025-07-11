namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a route part, either with no parameters or with all its parameters resolved.
/// </summary>
public interface IConcreteRoutePart : IEquatable<IConcreteRoutePart>
{
    /// <summary>
    /// Gets the route part that this concrete route is based on.
    /// </summary>
    public RoutePart RoutePart { get; }

    /// <summary>
    /// Gets the path string for this concrete route part.
    /// </summary>
    public string ToString();
}
