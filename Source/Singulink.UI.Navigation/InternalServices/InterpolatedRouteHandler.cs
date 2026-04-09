using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Provides a custom interpolated string handler for route strings.
/// </summary>
[InterpolatedStringHandler]
public class InterpolatedRouteHandler(int literalLength, int formattedCount)
{
    private readonly List<object> _routeParts = [];
    private string? _lambdaParameterName;

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
    public void AppendFormatted<T>([AllowNull] T holeValue, [CallerArgumentExpression(nameof(holeValue))] string holeExpr = "")
    {
        int dotIndex = holeExpr.IndexOf('.');
        string name;

        if (dotIndex >= 0)
        {
            // Dot notation: "p.FormId" → prefix "p", name "FormId"
            if (holeExpr.IndexOf('.', dotIndex + 1) >= 0)
                throw new FormatException($"Route hole expression '{holeExpr}' must contain at most one dot.");

            string prefix = holeExpr[..dotIndex];
            name = holeExpr[(dotIndex + 1)..];

            if (_lambdaParameterName is null)
                _lambdaParameterName = prefix;
            else if (_lambdaParameterName.Length is 0)
                throw new FormatException($"Route hole '{holeExpr}' uses dot notation, but previous holes did not. All holes must consistently use dot notation or not.");
            else if (_lambdaParameterName != prefix)
                throw new FormatException($"Route hole '{holeExpr}' references '{prefix}', but previous holes referenced '{_lambdaParameterName}'. All holes must use the same lambda parameter.");
        }
        else
        {
            // No dot: single parameter — use sentinel key
            name = RouteParamsHandler.SingleParamKey;

            if (_lambdaParameterName is null)
                _lambdaParameterName = string.Empty;
            else if (_lambdaParameterName.Length > 0)
                throw new FormatException($"Route hole '{holeExpr}' does not use dot notation, but previous holes used '{_lambdaParameterName}.Property' notation. All holes must be consistent.");
        }

        name = name.Trim();

        object lastPart = _routeParts.LastOrDefault();

        if (lastPart is RouteHole)
            throw new FormatException("Route parameters must be separated with a '/' separator.");

        if (lastPart is string literal && literal[^1] is not '/')
            throw new FormatException("Route path segments must be separated with a '/' separator.");

        if (_routeParts.OfType<RouteHole>().Any(h => h.Name == name))
        {
            if (name == RouteParamsHandler.SingleParamKey)
                throw new FormatException("Single-value parameter routes must have exactly one hole.");

            throw new FormatException($"Duplicate route hole '{name}'. Each hole must have a unique name.");
        }

        _routeParts.Add(new RouteHole(name, typeof(T)));
    }

    internal IReadOnlyList<object> Compile(string routeFuncExpr, bool isParamsModel)
    {
        // Remove leading path separator

        if (_routeParts.FirstOrDefault() is string firstLiteral && firstLiteral[0] is '/')
        {
            if (firstLiteral.Length is 1)
                _routeParts.RemoveAt(0);
            else
                _routeParts[0] = firstLiteral[1..];
        }

        // Remove trailing path separator

        if (_routeParts.LastOrDefault() is string lastLiteral && lastLiteral[^1] is '/')
        {
            if (lastLiteral.Length is 1)
                _routeParts.RemoveAt(_routeParts.Count - 1);
            else
                _routeParts[^1] = lastLiteral[..^1];
        }

        // Validate that hole expressions reference the correct lambda parameter

        if (_lambdaParameterName is not null && routeFuncExpr.Contains("=>"))
        {
            string expectedName = ExpressionParser.GetLambdaParameterName(routeFuncExpr);
            bool usesDotNotation = _lambdaParameterName.Length > 0;

            if (usesDotNotation)
            {
                // Dot notation: prefix before the dot must match the lambda parameter name
                if (_lambdaParameterName != expectedName)
                    throw new FormatException($"Route holes reference '{_lambdaParameterName}', but the lambda parameter is named '{expectedName}'.");

                if (!isParamsModel)
                    throw new FormatException($"Route holes use dot notation (e.g. '{expectedName}.Property'), but the parameter type is a single value, not a params model. Use the lambda parameter directly (e.g. '{expectedName}').");
            }
            else
            {
                // No dot notation: single parameter
                if (isParamsModel)
                    throw new FormatException($"Route holes use the lambda parameter directly (e.g. '{expectedName}'), but the parameter type is a params model. Use dot notation to specify properties (e.g. '{expectedName}.PropertyName').");
            }
        }

        return _routeParts;
    }
}
