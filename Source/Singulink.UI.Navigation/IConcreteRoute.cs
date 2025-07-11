namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a full concrete route.
/// </summary>
public interface IConcreteRoute
{
    /// <summary>
    /// Gets the options for the route, such as anchor or optional parameters.
    /// </summary>
    public RouteOptions Options { get; }

    /// <summary>
    /// Gets the concrete route parts that make up the route.
    /// </summary>
    public IReadOnlyList<IConcreteRoutePart> Parts { get; }

    /// <summary>
    /// Gets the path string that represents the route, without any options.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Returns a string representation of the route, including any options.
    /// </summary>
    public string ToString();
}
