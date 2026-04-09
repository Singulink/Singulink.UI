using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a route part.
/// </summary>
public abstract class RoutePart
{
    /// <summary>
    /// Gets the view model type associated with the route part.
    /// </summary>
    public Type ViewModelType { get; }

    internal Type? ParentViewModelType { get; }

    private protected RoutePart(Type viewModelType, Type? parentViewModelType)
    {
        ViewModelType = viewModelType;
        ParentViewModelType = parentViewModelType;
    }

    internal abstract bool TryMatch(ReadOnlySpan<char> routeString, RouteQuery query, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest);

    internal virtual IEnumerable<RoutePart> GetRegistrationParts() => [this];

    internal virtual void ValidateAsParent() { }
}

/// <summary>
/// Represents a route part with no parameters.
/// </summary>
public abstract class RoutePart<TViewModel> : RoutePart
    where TViewModel : class
{
    internal RouteBuilder RouteBuilder { get; }

    private protected RoutePart(RouteBuilder routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }
}

/// <summary>
/// Represents a parameterized route part.
/// </summary>
public abstract class RoutePart<TViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : RoutePart
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    private protected RoutePart(RouteBuilder<TParam> routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }

    internal override void ValidateAsParent() => RouteBuilder.ValidateAsParent();
}
