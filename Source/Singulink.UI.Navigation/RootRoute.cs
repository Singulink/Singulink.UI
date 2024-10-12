using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root route with no parameters.
/// </summary>
public class RootRoute<TViewModel> : RouteBase<TViewModel>, ISpecifiedRootRoute<TViewModel>
    where TViewModel : class
{
    /// <inheritdoc/>
    RouteBase ISpecifiedRoute.Route => this;

    internal RootRoute(RouteBuilder routeBuilder) : base(routeBuilder, null)
    {
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetRouteString();

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

    /// <inheritdoc/>
    bool IEquatable<ISpecifiedRoute>.Equals(ISpecifiedRoute? other) => other == this;
}

/// <summary>
/// Represents a root route with parameters.
/// </summary>
public class RootRoute<TParam, TViewModel> : RouteBase<TParam, TViewModel>
    where TParam : notnull
    where TViewModel : class, IRoutedViewModel<TParam>
{
    internal RootRoute(RouteBuilder<TParam> routeStringHandler) : base(routeStringHandler, null)
    {
    }

    /// <summary>
    /// Gets a specified route with the specified parameter (or tuple with multiple parameters).
    /// </summary>
    public ISpecifiedRootRoute<TViewModel> GetSpecified(TParam parameter)
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

    private class Specified : IParameterizedSpecifiedRoute<TParam, TViewModel>, ISpecifiedRootRoute<TViewModel>
    {
        private readonly TParam _parameter;
        private readonly RootRoute<TParam, TViewModel> _route;

        RouteBase ISpecifiedRoute.Route => _route;

        TParam IParameterizedSpecifiedRoute<TParam, TViewModel>.Parameter => _parameter;

        public Specified(RootRoute<TParam, TViewModel> route, TParam paramValue)
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
