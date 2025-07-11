using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Provides a custom interpolated string handler for route strings.
/// </summary>
[InterpolatedStringHandler]
public class InterpolatedRouteHandler(int literalLength, int formattedCount)
{
    private readonly List<object> _routeParts = [];

    /// <summary>
    /// Appends a literal path segment to the route.
    /// </summary>
    public void AppendLiteral(string literal)
    {
        if (literal.Length is 0)
            throw new FormatException("Route path segment cannot be empty.");

        if (literal.Any(char.IsWhiteSpace))
            throw new FormatException("Route path segment must not contain whitespace.");

        if (literal.Contains("//"))
            throw new FormatException("Route cannot contain empty path segments.");

        if (literal.Contains('#') || literal.Contains('?'))
            throw new FormatException("Route path segments must not contain query (?) or anchor (#) separators.");

        if (_routeParts.LastOrDefault() is string)
            throw new FormatException("Two literal route path segments must not appear next to each other.");

        if (_routeParts.Count > 0 && literal[0] is not '/')
            throw new FormatException("Route path segments must be separated with a '/' separator.");

        _routeParts.Add(literal);
    }

    /// <summary>
    /// Appends a formatted path segment to the route.
    /// </summary>
    public void AppendFormatted<T>([AllowNull] T t, [CallerArgumentExpression(nameof(t))] string tExpr = "")
        where T : notnull, IParsable<T>
    {
        if (tExpr.Any(char.IsWhiteSpace))
            throw new FormatException("Route parameter must not contain whitespace.");

        object lastPart = _routeParts.LastOrDefault();

        if (lastPart is RouteHole)
            throw new FormatException("Route parameters must be separated with a '/' separator.");

        if (lastPart is string literal && literal[^1] is not '/')
            throw new FormatException("Route path segments must be separated with a '/' separator.");

        _routeParts.Add(new RouteHole(tExpr, typeof(T)));
    }

    internal IEnumerable<object> Compile(List<string> lambdaParamNames)
    {
        if (formattedCount != lambdaParamNames.Count)
            throw new FormatException("Parameter count in route string does not match lambda parameter count.");

        // Remove leading path separator

        if (_routeParts.FirstOrDefault() is string firstLiteral && firstLiteral[0] is '/')
            if (firstLiteral.Length is 1)
                _routeParts.RemoveAt(0);
            else
                _routeParts[0] = firstLiteral[1..];

        // Remove trailing path separator

        if (_routeParts.LastOrDefault() is string lastLiteral && lastLiteral[^1] is '/')
            if (lastLiteral.Length is 1)
                _routeParts.RemoveAt(_routeParts.Count - 1);
            else
                _routeParts[^1] = lastLiteral[..^1];

        var remainingLambdaParamNames = lambdaParamNames.ToList();

        foreach (var hole in _routeParts.OfType<RouteHole>())
        {
            if (hole.Name != remainingLambdaParamNames[0])
            {
                if (lambdaParamNames.Contains(hole.Name))
                    throw new FormatException($"Incorrectly ordered parameter '{hole.Name}'. Parameters must appear in the same order in the lambda parameter list and the route.");

                throw new FormatException($"Unknown parameter '{hole.Name}' in route.");
            }

            remainingLambdaParamNames.RemoveAt(0);
        }

        if (remainingLambdaParamNames.Count is not 0)
            throw new FormatException($"Unused parameters in route: {string.Join(", ", remainingLambdaParamNames)}.");

        return _routeParts;
    }
}
