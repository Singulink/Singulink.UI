using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a builder for constructing routes.
/// </summary>
public abstract class RouteBuilderBase
{
    private readonly ImmutableArray<object> _routeParts;

    internal IReadOnlyList<string> HoleNames { get; }

    private protected RouteBuilderBase(IEnumerable<object> routeParts)
    {
        _routeParts = routeParts.ToImmutableArray();
        HoleNames = [.. _routeParts.OfType<RouteHole>().Select(h => h.Name)];
    }

    internal static ReadOnlySpan<char> PreProcessRouteString(ReadOnlySpan<char> route)
    {
        if (route is { Length: 0 } or "/")
            return [];

        if (route.IndexOf("//", StringComparison.Ordinal) >= 0) // always invalid, don't try to process
            return route;

        if (route is ['/', .. var r1])
            route = r1;

        if (route is [.. var r2, '/'])
            route = r2;

        return route;
    }

    /// <summary>
    /// Builds the path string from the supplied route values. If <paramref name="consumeHoleEntries"/> is <see langword="true"/>,
    /// hole entries are removed from the values so that the remaining entries can be treated as query parameters by the caller.
    /// Otherwise, hole lookups are non-consuming (used when the values must remain intact for subsequent parameter object creation).
    /// </summary>
    internal string BuildPath(RouteValuesCollection values, bool consumeHoleEntries)
    {
        var sb = new StringBuilder();

        foreach (object part in _routeParts)
        {
            if (part is string literal)
            {
                sb.Append(literal);
            }
            else if (part is RouteHole hole)
            {
                bool found = consumeHoleEntries
                    ? values.TryConsumeValue(hole.Name, out string? value)
                    : values.TryGetValue(hole.Name, out value);

                if (!found || value!.Length is 0)
                    throw new FormatException($"Route parameter '{hole.Name}' value cannot be null or empty.");

                sb.Append(Uri.EscapeDataString(value));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tries to match the route string, extracting path hole values into a list of (name, value) tuples.
    /// </summary>
    private protected bool TryMatchPath(ReadOnlySpan<char> route, out List<(string Key, string Value)>? pathParams, out ReadOnlySpan<char> rest)
    {
        rest = PreProcessRouteString(route);
        int initialLength = rest.Length;
        pathParams = null;

        foreach (object part in _routeParts)
        {
            if (part is string literal)
            {
                if (!rest.StartsWith(literal, StringComparison.Ordinal))
                    return false;

                rest = rest[literal.Length..];
            }
            else if (part is RouteHole hole)
            {
                int holeLength = rest.IndexOf('/');

                if (holeLength < 0)
                    holeLength = rest.Length;

                if (holeLength is 0)
                    return false;

                string value = Uri.UnescapeDataString(rest[..holeLength]);
                (pathParams ??= []).Add((hole.Name, value));
                rest = rest[holeLength..];
            }
        }

        // After consuming the route parts, the remainder must be empty or begin at a segment boundary ('/').
        // Otherwise a route like "a/{id}" matching against "a/123" with a child "b" route could allow a path
        // ending in a non-boundary literal to bleed into a sibling/child match (e.g. parent "a" + child "b" matching "ab").
        if (rest.Length < initialLength && rest.Length > 0 && rest[0] is not '/')
            return false;

        return true;
    }
}
