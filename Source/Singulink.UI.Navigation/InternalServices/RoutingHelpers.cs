namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Provides utility methods related to routing.
/// </summary>
public static class RoutingHelpers
{
    /// <summary>
    /// Gets the path represented by the specified route parts.
    /// </summary>
    public static string GetPath(IEnumerable<IConcreteRoutePart> routeParts)
    {
        return string.Join("/", routeParts.Select(r => r.ToString()).Where(r => r.Length > 0));
    }
}
