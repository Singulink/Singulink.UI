using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a group of routes that target the same parameter type. Add routes then call <see cref="Child{TParentViewModel,
/// TChildViewModel}"/> or <see cref="Root{TViewModel}"/> to create route part groups from this builder.
/// </summary>
/// <typeparam name="TParam">The full parameter type.</typeparam>
public class RouteGroupBuilder<[DynamicallyAccessedMembers(DAM.AllCtors)] TParam>
    where TParam : notnull
{
    private readonly List<RouteBuilder<TParam>> _builders = [];

    internal RouteGroupBuilder()
    {
    }

    /// <summary>
    /// Adds a route with no path parameters from the specified string.
    /// </summary>
    public RouteGroupBuilder<TParam> Add(string route)
    {
        _builders.Add(new RouteBuilder(route).ToParameterized<TParam>());
        return this;
    }

    /// <summary>
    /// Adds a parameterized route from the specified route function that returns the interpolated route.
    /// </summary>
    public RouteGroupBuilder<TParam> Add(
        Func<TParam, InterpolatedRouteHandler> routeFunc,
        [CallerArgumentExpression(nameof(routeFunc))] string routeFuncExpr = "")
    {
        _builders.Add(Route.Build(routeFunc, routeFuncExpr));
        return this;
    }

    /// <summary>
    /// Creates a root route part group for the specified view model type. Route builders are evaluated in order and the one with the most satisfied path
    /// holes is used when creating a concrete route.
    /// </summary>
    public RootRoutePart<TViewModel, TParam> Root<TViewModel>()
        where TViewModel : class, IRoutedViewModel<TParam>
    {
        ValidateBuilderCount();

        var candidates = _builders.Select(b => new DirectRootRoutePart<TViewModel, TParam>(b));
        return new RootRoutePartGroup<TViewModel, TParam>(candidates);
    }

    /// <summary>
    /// Creates a child route part group for the specified parent and child view model types.
    /// </summary>
    /// <exception cref="InvalidOperationException">Parent and child view models were the same type or fewer than two builders were added.</exception>
    public ChildRoutePart<TParentViewModel, TChildViewModel, TParam> Child<TParentViewModel, TChildViewModel>()
        where TParentViewModel : class, IRoutedViewModelBase
        where TChildViewModel : class, IRoutedViewModel<TParam>
    {
        if (typeof(TParentViewModel) == typeof(TChildViewModel))
            throw new InvalidOperationException("Parent and child view models cannot be the same type.");

        ValidateBuilderCount();

        var candidates = _builders.Select(b => new DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam>(b));
        return new ChildRoutePartGroup<TParentViewModel, TChildViewModel, TParam>(candidates);
    }

    private void ValidateBuilderCount()
    {
        if (_builders.Count < 2)
            throw new InvalidOperationException("A route group must contain at least two route builders.");
    }
}
