using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a route that can be navigated to.
/// </summary>
public abstract class RouteBase
{
    /// <summary>
    /// Gets the view model type associated with the route.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Type ViewModelType { get; }

    /// <summary>
    /// Gets the parent view model type associated with the route if it is a nested route, otherwise null.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Type? ParentViewModelType { get; }

    private protected RouteBase(Type viewModelType, Type? parentViewModelType)
    {
        ViewModelType = viewModelType;
        ParentViewModelType = parentViewModelType;
    }

    /// <summary>
    /// Internal use.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoute concreteRoute, out ReadOnlySpan<char> rest);

    /// <summary>
    /// Internal use.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual void InitializeViewModel(IRoutedViewModelBase viewModel, IConcreteRoute route) { }
}

/// <summary>
/// Represents a route that can be navigated to.
/// </summary>
public abstract class RouteBase<TViewModel> : RouteBase
    where TViewModel : class
{
    internal RouteBuilder RouteBuilder { get; }

    private protected RouteBase(RouteBuilder routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }
}

/// <summary>
/// Represents a route with parameters that can be navigated to.
/// </summary>
public abstract class RouteBase<TViewModel, TParam> : RouteBase
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    private protected RouteBase(RouteBuilder<TParam> routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void InitializeViewModel(IRoutedViewModelBase viewModel, IConcreteRoute route)
    {
        var vm = (TViewModel)viewModel;
        var parameterizedRoute = (IParameterizedConcreteRoute<TViewModel, TParam>)route;
        vm.Parameter = parameterizedRoute.Parameter;
    }

    internal string GetConcreteRouteString(TParam parameter) => RouteBuilder.GetRouteString(parameter);
}
