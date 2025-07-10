using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides methods for building routes.
/// </summary>
public static class Route
{
    /// <summary>
    /// Builds a route with no parameters from the specified string.
    /// </summary>
    public static RouteBuilder Build(string route)
    {
        InterpolatedRouteHandler r = new(route.Length, 0);
        r.AppendLiteral(route);
        var routeParts = r.Compile([]);
        return new RouteBuilder((string?)routeParts.SingleOrDefault());
    }

    /// <summary>
    /// Builds a route with a single parameter from the specified route function that returns the interpolated route.
    /// </summary>
    public static RouteBuilder<T> Build<T>(Func<T, InterpolatedRouteHandler> routeFunc, [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
        where T : notnull, IParsable<T>, IEquatable<T>
    {
        var paramNames = GetParamNamesFromLambda(routeFuncExpr, 1);
        var stringHandler = routeFunc(default!);
        var routeParts = stringHandler.Compile(paramNames);

        return new SingleParamRouteBuilder<T>(routeParts);
    }

    /// <summary>
    /// Builds a route with two parameters from the specified route function that returns the interpolated route.
    /// </summary>
    public static RouteBuilder<(T1 Param1, T2 Param2)> Build<T1, T2>(Func<T1, T2, InterpolatedRouteHandler> routeFunc, [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
        where T1 : notnull, IParsable<T1>, IEquatable<T1>
        where T2 : notnull, IParsable<T2>, IEquatable<T2>
    {
        var paramNames = GetParamNamesFromLambda(routeFuncExpr, 2);
        var stringHandler = routeFunc(default!, default!);
        var routeParts = stringHandler.Compile(paramNames);

        return new TupleRouteBuilder<T1, T2>(routeParts);
    }

    /// <summary>
    /// Builds a route with three parameters from the specified route function that returns the interpolated route.
    /// </summary>
    public static RouteBuilder<(T1 Param1, T2 Param2, T3 Param3)> Build<T1, T2, T3>(Func<T1, T2, T3, InterpolatedRouteHandler> routeFunc, [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
        where T1 : notnull, IParsable<T1>, IEquatable<T1>
        where T2 : notnull, IParsable<T2>, IEquatable<T2>
        where T3 : notnull, IParsable<T3>, IEquatable<T3>
    {
        var paramNames = GetParamNamesFromLambda(routeFuncExpr, 3);
        var stringHandler = routeFunc(default!, default!, default!);
        var routeParts = stringHandler.Compile(paramNames);

        return new TupleRouteBuilder<T1, T2, T3>(routeParts);
    }

    /// <summary>
    /// Builds a route with four parameters from the specified route function that returns the interpolated route.
    /// </summary>
    public static RouteBuilder<(T1 Param1, T2 Param2, T3 Param3, T4 Param4)> Build<T1, T2, T3, T4>(Func<T1, T2, T3, T4, InterpolatedRouteHandler> routeFunc, [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
        where T1 : notnull, IParsable<T1>, IEquatable<T1>
        where T2 : notnull, IParsable<T2>, IEquatable<T2>
        where T3 : notnull, IParsable<T3>, IEquatable<T3>
        where T4 : notnull, IParsable<T4>, IEquatable<T4>
    {
        var paramNames = GetParamNamesFromLambda(routeFuncExpr, 4);
        var stringHandler = routeFunc(default!, default!, default!, default!);
        var routeParts = stringHandler.Compile(paramNames);

        return new TupleRouteBuilder<T1, T2, T3, T4>(routeParts);
    }

    private static List<string> GetParamNamesFromLambda(string lambdaExpr, int expectedParamCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expectedParamCount, 0);

        int lambdaOpIndex = lambdaExpr.IndexOf("=>", StringComparison.Ordinal);

        if (lambdaOpIndex < 0)
            throw new ArgumentException($"No lambda operator in expression.", nameof(lambdaExpr));

        ReadOnlySpan<char> paramsPart = lambdaExpr.AsSpan()[..lambdaOpIndex].Trim();

        if (paramsPart.Length is 0)
            throw new ArgumentException("Lambda expression does not contain a parameter section.", nameof(lambdaExpr));

        if (paramsPart[0] is '(')
        {
            if (paramsPart[^1] is not ')')
                throw new ArgumentException($"Unmatched opening brace for lambda parameters: '{paramsPart}'", nameof(lambdaExpr));

            paramsPart = paramsPart[1..^1];
        }

        var paramNames = GetParamNames(paramsPart);

        if (paramNames.Count != expectedParamCount)
            throw new ArgumentException($"Expression did not contain {expectedParamCount} parameters in the parameter section: '{paramsPart}'");

        return paramNames;
    }

    private static List<string> GetParamNames(ReadOnlySpan<char> paramList)
    {
        List<string> paramNames = [];

        int genericDepth = 0;
        int tupleDepth = 0;
        int paramNameStartIndex = paramList.Length;
        bool startedType = false;

        for (int i = 0; i < paramList.Length; i++)
        {
            char c = paramList[i];

            if (c is '<')
            {
                genericDepth++;
            }
            else if (c is '>')
            {
                genericDepth--;
            }
            else if (c is '(')
            {
                tupleDepth++;
            }
            else if (c is ')')
            {
                tupleDepth--;
            }
            else if (c is ' ')
            {
                if (genericDepth is 0 && tupleDepth is 0 && startedType)
                {
                    while (paramList[++i] is ' ' && i < paramList.Length)
                    { }

                    paramNameStartIndex = i;

                    while (++i < paramList.Length && paramList[i] is not ',')
                    { }

                    var paramName = paramList[paramNameStartIndex..i].TrimEnd();

                    if (paramName.Length > 0)
                        paramNames.Add(paramName.ToString());

                    paramNameStartIndex = paramList.Length;

                    startedType = false;
                }
            }
            else
            {
                startedType = true;
            }
        }

        if (paramNameStartIndex < paramList.Length)
        {
            var paramName = paramList[paramNameStartIndex..].TrimEnd();

            if (paramName.Length > 0)
                paramNames.Add(paramName.ToString());
        }

        return paramNames;
    }
}
