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
    /// Gets the parameter (or parameters tuple, if there are multiple parameters) for this concrete route part, or <see langword="null"/> if the route part
    /// does not take a parameter.
    /// </summary>
    public object? Parameter { get; }

    /// <summary>
    /// Gets the path string for this concrete route part (without any query string).
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the query string parameters associated with this concrete route part. Always empty for non-leaf parts.
    /// </summary>
    public RouteQuery Query { get; }

    /// <summary>
    /// Returns the string representation of this concrete route part, including the path and query string (if any).
    /// </summary>
    public string ToString();
}
