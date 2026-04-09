using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a child route part with no parameters.
/// </summary>
public abstract class ChildRoutePart<TParentViewModel, TChildViewModel> : RoutePart<TChildViewModel>, IConcreteChildRoutePart<TParentViewModel, TChildViewModel>
    where TParentViewModel : class
    where TChildViewModel : class
{
    private protected ChildRoutePart(RouteBuilder routeBuilder) : base(routeBuilder, typeof(TParentViewModel))
    {
    }

    /// <inheritdoc/>
    RoutePart IConcreteRoutePart.RoutePart => this;

    /// <inheritdoc/>
    object? IConcreteRoutePart.Parameter => null;

    /// <inheritdoc/>
    string IConcreteRoutePart.Path => RouteBuilder.GetPartPath();

    /// <inheritdoc/>
    RouteQuery IConcreteRoutePart.Query => RouteQuery.Empty;

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

    /// <summary>
    /// Gets the route string.
    /// </summary>
    public override string ToString() => RouteBuilder.GetPartPath();

    /// <inheritdoc/>
    bool IEquatable<IConcreteRoutePart>.Equals(IConcreteRoutePart? other) => other == this;
}

internal class DirectChildRoutePart<TParentViewModel, TChildViewModel> : ChildRoutePart<TParentViewModel, TChildViewModel>
    where TParentViewModel : class
    where TChildViewModel : class
{
    internal DirectChildRoutePart(RouteBuilder routeBuilder) : base(routeBuilder)
    {
    }
}

/// <summary>
/// Represents a parameterized child route part.
/// </summary>
public abstract class ChildRoutePart<TParentViewModel, TChildViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : RoutePart
    where TParentViewModel : class
    where TChildViewModel : class
    where TParam : notnull
{
    private protected ChildRoutePart() : base(typeof(TChildViewModel), typeof(TParentViewModel))
    {
    }

    /// <summary>
    /// Gets a concrete route with the specified parameter.
    /// </summary>
    public abstract IConcreteChildRoutePart<TParentViewModel, TChildViewModel> ToConcrete(TParam parameter);
}

internal class DirectChildRoutePart<TParentViewModel, TChildViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : ChildRoutePart<TParentViewModel, TChildViewModel, TParam>
    where TParentViewModel : class
    where TChildViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    internal DirectChildRoutePart(RouteBuilder<TParam> routeBuilder)
    {
        RouteBuilder = routeBuilder;
    }

    public override IConcreteChildRoutePart<TParentViewModel, TChildViewModel> ToConcrete(TParam parameter)
    {
        var values = RouteBuilder.Handler.ToRouteValues(parameter);
        return ToConcrete(parameter, values);
    }

    internal IConcreteChildRoutePart<TParentViewModel, TChildViewModel> ToConcrete(TParam parameter, RouteValuesCollection values)
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

    private class Concrete : IConcreteChildRoutePart<TParentViewModel, TChildViewModel>, IParameterizedConcreteRoute<TChildViewModel, TParam>
    {
        public DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam> RoutePart { get; }

        public TParam Parameter { get; }

        public string Path { get; }

        public RouteQuery Query { get; }

        RoutePart IConcreteRoutePart.RoutePart => RoutePart;

        object? IConcreteRoutePart.Parameter => Parameter;

        public Concrete(DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam> routePart, TParam parameter, string path, RouteQuery query)
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
