namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a full concrete route.
/// </summary>
public sealed class NavigatorRoute
{
    /// <summary>
    /// Gets the anchor for the route, or <see langword="null"/> if no anchor is set.
    /// </summary>
    public string? Anchor { get; }

    /// <summary>
    /// Gets the concrete route parts that make up the route.
    /// </summary>
    public IReadOnlyList<IConcreteRoutePart> Parts => field ??= [.. Items.Select(i => i.ConcreteRoutePart)];

    /// <summary>
    /// Gets a value indicating whether the route is empty (i.e. has no route parts). This property can be used to determine whether the navigator has
    /// navigated to any route yet, since the navigator always starts with an empty route until the first navigation occurs.
    /// </summary>
    public bool IsEmpty => Items.Count is 0;

    internal IReadOnlyList<NavigationItem> Items { get; }

    /// <summary>
    /// Gets the path string that represents the route, without any query string or anchor.
    /// </summary>
    public string Path => field ??= string.Join("/", Items.Select(i => i.ConcreteRoutePart.Path).Where(p => p.Length > 0));

    internal NavigatorRoute(IReadOnlyList<NavigationItem> items, string? anchor)
    {
        Items = [.. items];
        Anchor = anchor;
    }

    /// <summary>
    /// Returns a string representation of the route, including the query string (from the leaf route part) and anchor.
    /// </summary>
    public override string ToString() => Route.GetRoute(Items.Select(i => i.ConcreteRoutePart), Anchor);
}
