using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.InternalServices;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides methods for building routes.
/// </summary>
public static class Route
{
    /// <summary>
    /// Builds a route with no path parameters from the specified string.
    /// </summary>
    public static RouteBuilder Build(string route)
    {
        InterpolatedRouteHandler r = new(route.Length, 0);
        r.AppendLiteral(route);
        var routeParts = r.Compile(string.Empty, isParamsModel: false);
        return new RouteBuilder((string?)routeParts.SingleOrDefault());
    }

    /// <summary>
    /// Builds a parameterized route from the specified route function that returns the interpolated route.
    /// </summary>
    public static RouteBuilder<TParam> Build<[DynamicallyAccessedMembers(DAM.AllCtors)] TParam>(
        Func<TParam, InterpolatedRouteHandler> routeFunc,
        [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
        where TParam : notnull
    {
        TParam param;

        if (typeof(TParam).IsValueType)
            param = default!;
        else if (typeof(TParam) == typeof(string))
            param = (TParam)(object)string.Empty;
        else
            param = (TParam)RuntimeHelpers.GetUninitializedObject(typeof(TParam))!;

        var stringHandler = routeFunc(param);
        var routeParts = stringHandler.Compile(routeFuncExpr, RouteParamsHandler<TParam>.Instance.IsParamsModel);

        return new RouteBuilder<TParam>(routeParts);
    }

    /// <summary>
    /// Creates a new route group builder for the specified parameter type.
    /// </summary>
    public static RouteGroupBuilder<TParam> BuildGroup<[DynamicallyAccessedMembers(DAM.AllCtors)] TParam>()
        where TParam : notnull
    {
        return new RouteGroupBuilder<TParam>();
    }

    /// <summary>
    /// Gets the route string represented by the specified route parts, optionally including an anchor.
    /// The query string is included for the leaf route part (if any).
    /// </summary>
    public static string GetRoute(IEnumerable<IConcreteRoutePart> routeParts, string? anchor = null)
    {
        string path = string.Join("/", routeParts.Select(r => r.ToString()).Where(r => r.Length > 0));
        return anchor is null ? path : path + "#" + anchor;
    }

    /// <summary>
    /// Gets the route string represented by the specified root route part.
    /// </summary>
    public static string GetRoute(IConcreteRootRoutePart rootRoutePart, string? anchor = null)
    {
        return GetRoute([rootRoutePart], anchor);
    }

    /// <summary>
    /// Gets the route string represented by the specified root and child route parts.
    /// </summary>
    public static string GetRoute<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        string? anchor = null)
        where TRootViewModel : class
    {
        return GetRoute([rootRoutePart, childRoutePart], anchor);
    }

    /// <summary>
    /// Gets the route string represented by the specified root and child route parts.
    /// </summary>
    public static string GetRoute<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        string? anchor = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        return GetRoute([rootRoutePart, childRoutePart1, childRoutePart2], anchor);
    }

    /// <summary>
    /// Gets the route string represented by the specified root and child route parts.
    /// </summary>
    public static string GetRoute<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        string? anchor = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        return GetRoute([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3], anchor);
    }

    /// <summary>
    /// Gets the partial route that has the same parts as the navigator's current route but with the specified anchor.
    /// </summary>
    public static string GetRoutePartial(INavigator navigator, string? anchor)
    {
        return GetRoute(navigator.CurrentRoute.Parts, anchor);
    }

    /// <summary>
    /// Gets the route string for the specified partial route. The current route must contain the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static string GetRoutePartial<TParentViewModel>(
        INavigator navigator,
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        string? anchor = null)
        where TParentViewModel : class
    {
        return GetRoute([..navigator.GetCurrentRoutePartsToParent(typeof(TParentViewModel)), childRoutePart], anchor);
    }

    /// <summary>
    /// Gets the route string for the specified partial route. The current route must contain the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static string GetRoutePartial<TParentViewModel, TChildViewModel1>(
        INavigator navigator,
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        string? anchor = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
    {
        return GetRoute([..navigator.GetCurrentRoutePartsToParent(typeof(TParentViewModel)), childRoutePart1, childRoutePart2], anchor);
    }

    /// <summary>
    /// Gets the route string for the specified partial route. The current route must contain the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static string GetRoutePartial<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        INavigator navigator,
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        string? anchor = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        return GetRoute([..navigator.GetCurrentRoutePartsToParent(typeof(TParentViewModel)), childRoutePart1, childRoutePart2, childRoutePart3], anchor);
    }
}
