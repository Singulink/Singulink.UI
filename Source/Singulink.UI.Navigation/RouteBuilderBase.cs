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

    private protected RouteBuilderBase(IEnumerable<object> routeParts) => _routeParts = routeParts.ToImmutableArray();

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

    private protected void AddIfLiteral(ref int partIndex, StringBuilder sb)
    {
        if (partIndex < _routeParts.Length && _routeParts[partIndex] is string literal)
        {
            sb.Append(literal);
            partIndex++;
        }
    }

    private protected void AddIfLiteralThenAddHole<T>(ref int partIndex, T value, StringBuilder sb) where T : notnull
    {
        if (_routeParts[partIndex++] is not RouteHole hole || hole.HoleType != typeof(T))
            throw new UnreachableException("Unexpected hole type.");

        int preLength = sb.Length;
        sb.Append(Uri.EscapeDataString(value.ToString() ?? string.Empty));

        if (preLength == sb.Length)
            throw new FormatException("Route parameter values cannot be empty.");
    }

    private protected bool MatchIfLiteral(ref int partIndex, ref ReadOnlySpan<char> remainingRoute)
    {
        if (partIndex < _routeParts.Length && _routeParts[partIndex] is string literal)
        {
            if (remainingRoute.StartsWith(literal, StringComparison.Ordinal))
            {
                remainingRoute = remainingRoute[literal.Length..];
                partIndex++;
                return true;
            }

            return false;
        }

        return true;
    }

    private protected bool MatchIfLiteralThenMatchHole<T>(ref int partIndex, ref ReadOnlySpan<char> remainingRoute, [MaybeNullWhen(false)] out T value)
        where T : IParsable<T>
    {
        if (!MatchIfLiteral(ref partIndex, ref remainingRoute))
        {
            value = default;
            return false;
        }

        return MatchHole(ref partIndex, ref remainingRoute, out value);
    }

    private protected void EnsurePartIndexAtEnd(int partIndex)
    {
        if (partIndex != _routeParts.Length)
            throw new UnreachableException("Unexpected ending route parts index.");
    }

    private void AddHole<T>(ref int index, T value, StringBuilder sb) where T : notnull
    {
        if (_routeParts[index++] is not RouteHole hole || hole.HoleType != typeof(T))
            throw new UnreachableException("Unexpected hole type.");

        int preLength = sb.Length;
        sb.Append(Uri.EscapeDataString(value.ToString() ?? string.Empty));

        if (preLength == sb.Length)
            throw new FormatException("Route parameter values cannot be empty.");
    }

    private bool MatchHole<T>(ref int partIndex, ref ReadOnlySpan<char> remainingRoute, [MaybeNullWhen(false)] out T value)
        where T : IParsable<T>
    {
        if (_routeParts[partIndex] is not RouteHole hole || hole.HoleType != typeof(T))
            throw new UnreachableException($"Unexpected hole type.");

        int holeLength = remainingRoute.IndexOf('/');

        if (holeLength < 0)
            holeLength = remainingRoute.Length;

        if (holeLength is 0)
        {
            value = default;
            return false;
        }

        string unescapedHoleString = Uri.UnescapeDataString(remainingRoute[..holeLength].ToString());

        if (T.TryParse(unescapedHoleString, CultureInfo.InvariantCulture, out value))
        {
            remainingRoute = remainingRoute[holeLength..];
            partIndex++;
            return true;
        }

        return false;
    }
}
