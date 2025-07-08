using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root route with no parameters.
/// </summary>
public class RootRoute<TViewModel> : RouteBase<TViewModel>, IConcreteRootRoute<TViewModel>
    where TViewModel : class
{
    /// <inheritdoc/>
    RouteBase IConcreteRoute.Route => this;

    internal RootRoute(RouteBuilder routeBuilder) : base(routeBuilder, null)
    {
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetRouteString();

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

    /// <inheritdoc/>
    bool IEquatable<IConcreteRoute>.Equals(IConcreteRoute? other) => other == this;
}

/// <summary>
/// Represents a root route with parameters.
/// </summary>
public class RootRoute<TViewModel, TParam> : RouteBase<TViewModel, TParam>
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RootRoute(RouteBuilder<TParam> routeStringHandler) : base(routeStringHandler, null)
    {
    }

    /// <summary>
    /// Gets a concrete route using the specified parameter (or a tuple of parameters, if there are multiple parameters).
    /// </summary>
    public IConcreteRootRoute<TViewModel> GetConcrete(TParam parameter)
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

    private class Concrete : IParameterizedConcreteRoute<TViewModel, TParam>, IConcreteRootRoute<TViewModel>
    {
        private readonly TParam _parameter;
        private readonly RootRoute<TViewModel, TParam> _route;

        RouteBase IConcreteRoute.Route => _route;

        TParam IParameterizedConcreteRoute<TViewModel, TParam>.Parameter => _parameter;

        public Concrete(RootRoute<TViewModel, TParam> route, TParam paramValue)
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
