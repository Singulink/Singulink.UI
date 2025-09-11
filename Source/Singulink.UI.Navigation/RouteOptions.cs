namespace Singulink.UI.Navigation;

/// <summary>
/// Represents options for a route.
/// </summary>
public sealed record RouteOptions : IEquatable<RouteOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RouteOptions"/> class with the specified anchor.
    /// </summary>
    /// <param name="anchor">The anchor for the route.</param>
    public RouteOptions(string? anchor = null)
    {
        Anchor = anchor;
    }

    /// <summary>
    /// Gets an empty route options instance.
    /// </summary>
    public static RouteOptions Empty { get; } = new();

    /// <summary>
    /// Gets the anchor for the route.
    /// </summary>
    public string? Anchor { get; }

    /// <summary>
    /// Returns a string representation of the route options.
    /// </summary>
    public override string ToString() => Anchor is null ? string.Empty : "#" + Anchor;
}
