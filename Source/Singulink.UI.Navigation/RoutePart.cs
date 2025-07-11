using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a route part.
/// </summary>
public abstract class RoutePart
{
    /// <summary>
    /// Gets the view model type associated with the route.
    /// </summary>
    public Type ViewModelType { get; }

    internal Type? ParentViewModelType { get; }

    private protected RoutePart(Type viewModelType, Type? parentViewModelType)
    {
        ViewModelType = viewModelType;
        ParentViewModelType = parentViewModelType;
    }

    internal abstract bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest);

    internal virtual void InitializeViewModel(IRoutedViewModelBase viewModel, INavigator navigator, IConcreteRoutePart route)
    {
        MixinManager.SetNavigator(viewModel, navigator);
    }
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
public abstract class RoutePart<TViewModel, TParam> : RoutePart
    where TViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    private protected RoutePart(RouteBuilder<TParam> routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }

    internal override void InitializeViewModel(IRoutedViewModelBase viewModel, INavigator navigator, IConcreteRoutePart route)
    {
        base.InitializeViewModel(viewModel, navigator, route);

        var vm = (TViewModel)viewModel;
        var parameterizedRoute = (IParameterizedConcreteRoute<TViewModel, TParam>)route;
        MixinManager.SetParameter(vm, parameterizedRoute.Parameter);
    }

    internal string GetConcreteRouteString(TParam parameter) => RouteBuilder.GetPartPath(parameter);
}
