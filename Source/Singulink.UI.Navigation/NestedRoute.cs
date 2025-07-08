using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a nested route without a parameter.
/// </summary>
public class NestedRoute<TParentViewModel, TNestedViewModel> : RouteBase<TNestedViewModel>, IConcreteNestedRoute<TParentViewModel, TNestedViewModel>
    where TNestedViewModel : class
{
    internal NestedRoute(RouteBuilder routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <inheritdoc/>
    RouteBase IConcreteRoute.Route => this;

    /// <inheritdoc/>
    public override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoute concreteRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, out rest))
        {
            concreteRoute = this;
            return true;
        }

        concreteRoute = default;
        return false;
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetRouteString();

    /// <inheritdoc/>
    bool IEquatable<IConcreteRoute>.Equals(IConcreteRoute? other) => other == this;
}

/// <summary>
/// Represents a nested route with a parameter.
/// </summary>
public class NestedRoute<TParentViewModel, TNestedViewModel, TParam> : RouteBase<TNestedViewModel, TParam>
    where TNestedViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal NestedRoute(RouteBuilder<TParam> routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <summary>
    /// Gets a concrete route with the specified parameter (or a tuple of parameters, if there are multiple parameters).
    /// </summary>
    public IConcreteNestedRoute<TParentViewModel, TNestedViewModel> GetConcrete(TParam parameter)
    {
        return new Concrete(this, parameter);
    }

    /// <inheritdoc/>
    public override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoute concreteRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, out TParam parameter, out rest))
        {
            concreteRoute = new Concrete(this, parameter);
            return true;
        }

        concreteRoute = default;
        return false;
    }

    private class Concrete : IConcreteNestedRoute<TParentViewModel, TNestedViewModel>, IParameterizedConcreteRoute<TNestedViewModel, TParam>
    {
        private readonly NestedRoute<TParentViewModel, TNestedViewModel, TParam> _route;
        private readonly TParam _parameter;

        RouteBase IConcreteRoute.Route => _route;

        TParam IParameterizedConcreteRoute<TNestedViewModel, TParam>.Parameter => _parameter;

        public Concrete(NestedRoute<TParentViewModel, TNestedViewModel, TParam> route, TParam paramValue)
        {
            _route = route;
            _parameter = paramValue;
        }

        public override string ToString() => _route.GetConcreteRouteString(_parameter);

        bool IEquatable<IConcreteRoute>.Equals(IConcreteRoute? other)
        {
            return other is Concrete specified && specified._route == _route && specified._parameter.Equals(_parameter);
        }
    }
}
