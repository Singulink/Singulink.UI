using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root route part with no parameters.
/// </summary>
public class RootRoutePart<TViewModel> : RoutePart<TViewModel>, IConcreteRootRoutePart<TViewModel>
    where TViewModel : class
{
    /// <inheritdoc/>
    RoutePart IConcreteRoutePart.RoutePart => this;

    internal RootRoutePart(RouteBuilder routeBuilder) : base(routeBuilder, null)
    {
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetPartPath();

    internal override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest)
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
    bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other) => other == this;
}

/// <summary>
/// Represents a parameterized root route part.
/// </summary>
public class RootRoutePart<TViewModel, TParam> : RoutePart<TViewModel, TParam>
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RootRoutePart(RouteBuilder<TParam> routeStringHandler) : base(routeStringHandler, null)
    {
    }

    /// <summary>
    /// Gets a concrete route using the specified parameter (or parameters tuple, if there are multiple parameters).
    /// </summary>
    public IConcreteRootRoutePart<TViewModel> ToConcrete(TParam parameter)
    {
        return new Concrete(this, parameter);
    }

    internal override bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, out TParam parameter, out rest))
        {
            concreteRoute = new Concrete(this, parameter);
            return true;
        }

        concreteRoute = default;
        return false;
    }

    private class Concrete : IParameterizedConcreteRoute<TViewModel, TParam>, IConcreteRootRoutePart<TViewModel>
    {
        public RootRoutePart<TViewModel, TParam> RoutePart { get; }

        public TParam Parameter { get; }

        RoutePart IConcreteRoutePart.RoutePart => RoutePart;

        public Concrete(RootRoutePart<TViewModel, TParam> routePart, TParam paramValue)
        {
            RoutePart = routePart;
            Parameter = paramValue;
        }

        public override string ToString() => RoutePart.GetConcreteRouteString(Parameter);

        bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other)
        {
            return other is Concrete concrete && concrete.RoutePart == RoutePart && concrete.Parameter.Equals(Parameter);
        }
    }
}
