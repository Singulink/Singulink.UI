using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root route part with no parameters.
/// </summary>
public abstract class RootRoutePart<TViewModel> : RoutePart<TViewModel>, IConcreteRootRoutePart<TViewModel>
    where TViewModel : class
{
    /// <inheritdoc/>
    RoutePart IConcreteRoutePart.RoutePart => this;

    /// <inheritdoc/>
    object? IConcreteRoutePart.Parameter => null;

    /// <inheritdoc/>
    string IConcreteRoutePart.Path => RouteBuilder.GetPartPath();

    /// <inheritdoc/>
    RouteQuery IConcreteRoutePart.Query => RouteQuery.Empty;

    private protected RootRoutePart(RouteBuilder routeBuilder) : base(routeBuilder, null)
    {
    }

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetPartPath();

    internal override bool TryMatch(ReadOnlySpan<char> routeString, RouteQuery query, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest)
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

internal class DirectRootRoutePart<TViewModel> : RootRoutePart<TViewModel>
    where TViewModel : class
{
    internal DirectRootRoutePart(RouteBuilder routeBuilder) : base(routeBuilder)
    {
    }
}

/// <summary>
/// Represents a parameterized root route part.
/// </summary>
public abstract class RootRoutePart<TViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : RoutePart
    where TViewModel : class
    where TParam : notnull
{
    private protected RootRoutePart() : base(typeof(TViewModel), null)
    {
    }

    /// <summary>
    /// Gets a concrete route using the specified parameter.
    /// </summary>
    public abstract IConcreteRootRoutePart<TViewModel> ToConcrete(TParam parameter);
}

internal class DirectRootRoutePart<TViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : RootRoutePart<TViewModel, TParam>
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    internal DirectRootRoutePart(RouteBuilder<TParam> routeBuilder)
    {
        RouteBuilder = routeBuilder;
    }

    public override IConcreteRootRoutePart<TViewModel> ToConcrete(TParam parameter)
    {
        var values = RouteBuilder.Handler.ToRouteValues(parameter);
        return ToConcrete(parameter, values);
    }

    internal IConcreteRootRoutePart<TViewModel> ToConcrete(TParam parameter, RouteValuesCollection values)
    {
        string path = RouteBuilder.BuildPath(values, consumeHoleEntries: true);
        var query = values.ConsumeQuery();
        return new Concrete(this, parameter, path, query);
    }

    internal override bool TryMatch(ReadOnlySpan<char> routeString, RouteQuery query, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest)
    {
        if (RouteBuilder.TryMatch(routeString, query, out TParam parameter, out string? path, out rest))
        {
            concreteRoute = new Concrete(this, parameter, path, query);
            return true;
        }

        concreteRoute = default;
        return false;
    }

    private class Concrete : IParameterizedConcreteRoute<TViewModel, TParam>, IConcreteRootRoutePart<TViewModel>
    {
        public DirectRootRoutePart<TViewModel, TParam> RoutePart { get; }

        public TParam Parameter { get; }

        public string Path { get; }

        public RouteQuery Query { get; }

        RoutePart IConcreteRoutePart.RoutePart => RoutePart;

        object? IConcreteRoutePart.Parameter => Parameter;

        public Concrete(DirectRootRoutePart<TViewModel, TParam> routePart, TParam parameter, string path, RouteQuery query)
        {
            RoutePart = routePart;
            Parameter = parameter;
            Path = path;
            Query = query;
        }

        public override string ToString() => Query.Count is 0 ? Path : $"{Path}?{Query}";

        bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other)
        {
            return other is Concrete concrete && concrete.RoutePart == RoutePart && concrete.Parameter.Equals(Parameter);
        }
    }
}
