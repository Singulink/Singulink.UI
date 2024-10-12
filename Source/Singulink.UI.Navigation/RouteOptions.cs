namespace Singulink.UI.Navigation;

/// <summary>
/// Represents options for a route.
/// </summary>
public sealed class RouteOptions(string? anchor = null) : IEquatable<RouteOptions>
{
    private string? _strValue;

    /// <summary>
    /// Gets an empty route options instance.
    /// </summary>
    public static RouteOptions Empty { get; } = new();

    /// <summary>
    /// Determines whether two route options are equal.
    /// </summary>
    public static bool operator ==(RouteOptions? left, RouteOptions? right) => left?.Equals(right) ?? right is null;

    /// <summary>
    /// Determines whether two route options are not equal.
    /// </summary>
    public static bool operator !=(RouteOptions? left, RouteOptions? right) => !(left == right);

    /// <summary>
    /// Gets the anchor for the route.
    /// </summary>
    public string? Anchor => anchor;

    /// <summary>
    /// Determines whether the specified route is equal to the current route.
    /// </summary>
    public bool Equals(RouteOptions? other) => other != null && (ReferenceEquals(this, other) || ToString() == other.ToString());

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RouteOptions other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Anchor);

    /// <summary>
    /// Returns a string representation of the route options.
    /// </summary>
    public override string ToString() => _strValue ??= anchor is null ? string.Empty : $"#{Anchor}";
}
