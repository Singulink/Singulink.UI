using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a builder used for constructing routes to view models without parameters.
/// </summary>
public class RouteBuilder
{
    private readonly string _route;

    internal RouteBuilder(string? route) => _route = route ?? string.Empty;

    /// <summary>
    /// Creates a root route part for the specified view model type.
    /// </summary>
    public RootRoutePart<TViewModel> Root<TViewModel>()
        where TViewModel : class, IRoutedViewModel
    {
        return new DirectRootRoutePart<TViewModel>(this);
    }

    /// <summary>
    /// Creates a child route part for the specified parent and child view model type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Parent and child view models were the same type.</exception>
    public ChildRoutePart<TParentViewModel, TChildViewModel> Child<TParentViewModel, TChildViewModel>()
        where TParentViewModel : class, IRoutedViewModelBase
        where TChildViewModel : class, IRoutedViewModel
    {
        if (typeof(TParentViewModel) == typeof(TChildViewModel))
            throw new InvalidOperationException("Parent and child view models cannot be the same type.");

        return new DirectChildRoutePart<TParentViewModel, TChildViewModel>(this);
    }

    internal string GetPartPath() => _route;

    internal bool TryMatch(ReadOnlySpan<char> route, out ReadOnlySpan<char> rest)
    {
        rest = RouteBuilderBase.PreProcessRouteString(route);

        if (!rest.StartsWith(_route, StringComparison.Ordinal))
            return false;

        rest = rest[_route.Length..];

        // After consuming a literal, the remainder must be empty or begin at a segment boundary ('/').
        // Otherwise "ab" would match a parent literal "a" followed by a child literal "b".
        if (_route.Length > 0 && rest.Length > 0 && rest[0] is not '/')
            return false;

        return true;
    }

    internal RouteBuilder<T> ToParameterized<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T>() where T : notnull
    {
        return new RouteBuilder<T>([GetPartPath()]);
    }
}

/// <summary>
/// Represents a builder used for constructing routes to view models with parameters.
/// </summary>
public class RouteBuilder<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : RouteBuilderBase
    where TParam : notnull
{
    private readonly RouteParamsHandler<TParam> _handler = RouteParamsHandler<TParam>.Instance;

    internal RouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    /// <summary>
    /// Creates a root route part for the specified view model type.
    /// </summary>
    public RootRoutePart<TViewModel, TParam> Root<TViewModel>()
        where TViewModel : class, IRoutedViewModel<TParam>
    {
        return new DirectRootRoutePart<TViewModel, TParam>(this);
    }

    /// <summary>
    /// Creates a child route part for the specified parent and child view model type.
    /// </summary>
    public ChildRoutePart<TParentViewModel, TChildViewModel, TParam> Child<TParentViewModel, TChildViewModel>()
        where TParentViewModel : class, IRoutedViewModelBase
        where TChildViewModel : class, IRoutedViewModel<TParam>
    {
        return new DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam>(this);
    }

    internal RouteParamsHandler<TParam> Handler => _handler;

    internal bool TryMatch(ReadOnlySpan<char> route, RouteQuery query, [MaybeNullWhen(false)] out TParam parameter, [MaybeNullWhen(false)] out string path, out ReadOnlySpan<char> rest)
    {
        if (!TryMatchPath(route, out var pathParams, out rest))
        {
            parameter = default;
            path = null;
            return false;
        }

        var values = new RouteValuesCollection(pathParams, query);

        // Build the path non-consuming so holes remain available for the handler to consume.
        path = BuildPath(values, consumeHoleEntries: false);

        if (!_handler.TryCreate(values, out parameter))
        {
            parameter = default;
            path = null;
            return false;
        }

        return true;
    }

    internal bool AreAllHolesSatisfied(RouteValuesCollection values)
    {
        foreach (string name in HoleNames)
        {
            if (!values.TryGetValue(name, out _))
                return false;
        }

        return true;
    }

    internal void ValidateAsParent()
    {
        foreach (string name in _handler.RequiredParameterNames)
        {
            if (!HoleNames.Contains(name))
            {
                throw new InvalidOperationException(
                    $"Required parameter '{name}' of type '{typeof(TParam)}' is not a path hole in the route. " +
                    $"Parent routes with child routes must have all required parameters as path holes.");
            }
        }

        if (_handler.ProvidesQueryAccess)
        {
            throw new InvalidOperationException(
                $"Parameter type '{typeof(TParam)}' provides query string access and cannot be used for a view model with child routes. " +
                $"Query string parameters are only available to leaf view models.");
        }
    }
}
