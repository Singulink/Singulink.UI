using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a nested route without a parameter.
/// </summary>
public class NestedRoute<TParentViewModel, TNestedViewModel> : RouteBase<TNestedViewModel>, ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel>
    where TNestedViewModel : class
{
    internal NestedRoute(RouteBuilder routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <inheritdoc/>
    RouteBase ISpecifiedRoute.Route => this;

    /// <inheritdoc/>
    public override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out ISpecifiedRoute specifiedRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, out rest))
        {
            specifiedRoute = this;
            return true;
        }

        specifiedRoute = default;
        return false;
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetRouteString();

    /// <inheritdoc/>
    bool IEquatable<ISpecifiedRoute>.Equals(ISpecifiedRoute? other) => other == this;
}

/// <summary>
/// Represents a nested route with a parameter.
/// </summary>
public class NestedRoute<TParentViewModel, TParam, TNestedViewModel> : RouteBase<TParam, TNestedViewModel>
    where TParam : notnull
    where TNestedViewModel : class, IRoutedViewModel<TParam>
{
    internal NestedRoute(RouteBuilder<TParam> routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <summary>
    /// Gets a specified route with the specified parameter.
    /// </summary>
    public ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> GetSpecified(TParam parameter)
    {
        return new Specified(this, parameter);
    }

    /// <inheritdoc/>
    public override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out ISpecifiedRoute specifiedRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, out TParam parameter, out rest))
        {
            specifiedRoute = new Specified(this, parameter);
            return true;
        }

        specifiedRoute = default;
        return false;
    }

    private class Specified : ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel>, IParameterizedSpecifiedRoute<TParam, TNestedViewModel>
    {
        private readonly NestedRoute<TParentViewModel, TParam, TNestedViewModel> _route;
        private readonly TParam _parameter;

        RouteBase ISpecifiedRoute.Route => _route;

        TParam IParameterizedSpecifiedRoute<TParam, TNestedViewModel>.Parameter => _parameter;

        public Specified(NestedRoute<TParentViewModel, TParam, TNestedViewModel> route, TParam paramValue)
        {
            _route = route;
            _parameter = paramValue;
        }

        public override string ToString() => _route.GetSpecifiedRouteString(_parameter);

        bool IEquatable<ISpecifiedRoute>.Equals(ISpecifiedRoute? other)
        {
            return other is Specified specified && specified._route == _route && specified._parameter.Equals(_parameter);
        }
    }
}
