namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a full concrete route.
/// </summary>
/// <remarks>
/// Routes obtained from the navigator (e.g. <see cref="INavigator.CurrentRoute"/> or the navigation stacks) are live views of the navigator's route entries:
/// their parts and anchor reflect any subsequent in-place updates made with <see cref="INavigator.UpdateCurrentRoute(string?)"/> and its overloads. Use <see
/// cref="ToString"/> to capture a snapshot of the route at a point in time.
/// </remarks>
public sealed class NavigatorRoute
{
    // Cached views of the items and anchor, cleared whenever the route is updated in place.
    private IReadOnlyList<IConcreteRoutePart>? _parts;
    private string? _path;
    private string? _routeString;

    /// <summary>
    /// Gets the anchor for the route, or <see langword="null"/> if no anchor is set.
    /// </summary>
    public string? Anchor { get; private set; }

    /// <summary>
    /// Gets the concrete route parts that make up the route.
    /// </summary>
    public IReadOnlyList<IConcreteRoutePart> Parts => _parts ??= [.. Items.Select(i => i.ConcreteRoutePart)];

    /// <summary>
    /// Gets a value indicating whether the route is empty (i.e. has no route parts). This property can be used to determine whether the navigator has
    /// navigated to any route yet, since the navigator always starts with an empty route until the first navigation occurs.
    /// </summary>
    public bool IsEmpty => Items.Count is 0;

    internal IReadOnlyList<NavigationItem> Items { get; }

    /// <summary>
    /// Gets a number that is incremented every time the route is updated in place, so that change tracking can detect updates to the same instance.
    /// </summary>
    internal int Version { get; private set; }

    /// <summary>
    /// Gets the path string that represents the route, without any query string or anchor.
    /// </summary>
    public string Path => _path ??= string.Join("/", Items.Select(i => i.ConcreteRoutePart.Path).Where(p => p.Length > 0));

    internal NavigatorRoute(IReadOnlyList<NavigationItem> items, string? anchor)
    {
        Items = [.. items];
        Anchor = anchor;
    }

    /// <summary>
    /// Updates the anchor in place.
    /// </summary>
    internal void Update(string? anchor)
    {
        Anchor = anchor;
        Version++;

        _routeString = null;
    }

    /// <summary>
    /// Updates the leaf route part and anchor in place.
    /// </summary>
    internal void Update(IConcreteRoutePart leafRoutePart, string? anchor)
    {
        Items[^1].ConcreteRoutePart = leafRoutePart;
        Anchor = anchor;
        Version++;

        _parts = null;
        _path = null;
        _routeString = null;
    }

    /// <summary>
    /// Returns a string representation of the route, including the query string (from the leaf route part) and anchor.
    /// </summary>
    public override string ToString() => _routeString ??= Route.GetRoute(Items.Select(i => i.ConcreteRoutePart), Anchor);
}
