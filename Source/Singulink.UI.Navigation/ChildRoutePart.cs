using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a child route part with no parameters.
/// </summary>
public class ChildRoutePart<TParentViewModel, TChildViewModel> : RoutePart<TChildViewModel>, IConcreteChildRoutePart<TParentViewModel, TChildViewModel>
    where TParentViewModel : class
    where TChildViewModel : class
{
    internal ChildRoutePart(RouteBuilder routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <inheritdoc/>
    RoutePart IConcreteRoutePart.RoutePart => this;

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

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetPartPath();

    /// <inheritdoc/>
    bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other) => other == this;
}

/// <summary>
/// Represents a parameterized child route part.
/// </summary>
public class ChildRoutePart<TParentViewModel, TChildViewModel, TParam> : RoutePart<TChildViewModel, TParam>
    where TParentViewModel : class
    where TChildViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal ChildRoutePart(RouteBuilder<TParam> routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <summary>
    /// Gets a concrete route with the specified parameter (or parameters tuple, if there are multiple parameters).
    /// </summary>
    public IConcreteChildRoutePart<TParentViewModel, TChildViewModel> ToConcrete(TParam parameter)
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

    private class Concrete : IConcreteChildRoutePart<TParentViewModel, TChildViewModel>, IParameterizedConcreteRoute<TChildViewModel, TParam>
    {
        public ChildRoutePart<TParentViewModel, TChildViewModel, TParam> RoutePart { get; }

        public TParam Parameter { get; }

        RoutePart IConcreteRoutePart.RoutePart => RoutePart;

        public Concrete(ChildRoutePart<TParentViewModel, TChildViewModel, TParam> routePart, TParam paramValue)
        {
            RoutePart = routePart;
            Parameter = paramValue;
        }

        public override string ToString() => RoutePart.GetConcreteRouteString(Parameter);

        bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other)
        {
            return other is Concrete specified && specified.RoutePart == RoutePart && specified.Parameter.Equals(Parameter);
        }
    }
}
