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
    public Type ViewModelType { get; }

    /// <summary>
    /// Gets the parent view model type associated with the route if it is a nested route, otherwise null.
    /// </summary>
    public Type? ParentViewModelType { get; }

    private protected RouteBase(Type viewModelType, Type? parentViewModelType)
    {
        ViewModelType = viewModelType;
        ParentViewModelType = parentViewModelType;
    }

    /// <summary>
    /// Attempts to match the specified route string to this route.
    /// </summary>
    /// <param name="routeString">The route string to match.</param>
    /// <param name="specifiedRoute">If successful, outputs the matched specified route, otherwise <see langword="null"/>.</param>
    /// <param name="rest">Outputs the unmatched portion of the route.</param>
    public abstract bool TryMatch(ReadOnlySpan<char> routeString, [MaybeNullWhen(false)] out ISpecifiedRoute specifiedRoute, out ReadOnlySpan<char> rest);

    /// <summary>
    /// Invokes the OnNavigatedTo method on the routed view model associated with the specified view using the info from the specified route and navigation
    /// arguments.
    /// </summary>
    public abstract Task InvokeViewModelOnNavigatedToAsync(INavigator navigator, object view, ISpecifiedRoute route, NavigationArgs args);
}

/// <summary>
/// Represents a route that can be navigated to.
/// </summary>
public abstract class RouteBase<TViewModel> : RouteBase
    where TViewModel : IRoutedViewModel
{
    internal RouteBuilder RouteBuilder { get; }

    private protected RouteBase(RouteBuilder routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }

    /// <inheritdoc />
    public override async Task InvokeViewModelOnNavigatedToAsync(INavigator navigator, object view, ISpecifiedRoute route, NavigationArgs args)
    {
        var typedView = (IRoutedView<TViewModel>)view;
        await typedView.Model.OnNavigatedToAsync(navigator, args);
    }
}

/// <summary>
/// Represents a route with parameters that can be navigated to.
/// </summary>
public abstract class RouteBase<TParam, TViewModel> : RouteBase
    where TParam : notnull
    where TViewModel : IRoutedViewModel<TParam>
{
    internal RouteBuilder<TParam> RouteBuilder { get; }

    private protected RouteBase(RouteBuilder<TParam> routeBuilder, Type? parentViewModelType) : base(typeof(TViewModel), parentViewModelType)
    {
        RouteBuilder = routeBuilder;
    }

    /// <inheritdoc />
    public override Task InvokeViewModelOnNavigatedToAsync(INavigator navigator, object view, ISpecifiedRoute route, NavigationArgs args)
    {
        var typedView = (IRoutedView<TViewModel>)view;
        var parameterizedRoute = (IParameterizedSpecifiedRoute<TParam, TViewModel>)route;

        return typedView.Model.OnNavigatedToAsync(navigator, parameterizedRoute.Parameter, args);
    }

    internal string GetSpecifiedRouteString(TParam parameter) => RouteBuilder.GetRouteString(parameter);
}
