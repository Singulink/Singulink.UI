using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.WinUI;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides the nested <see cref="RouteInfo"/> and <see cref="RouteInfoItem"/> classes.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.GetRouteOptions"/>
    public RouteOptions GetRouteOptions()
    {
        EnsureThreadAccess();
        return CurrentRouteInfo?.Options ?? RouteOptions.Empty;
    }

    /// <inheritdoc cref="INavigator.TryGetRouteParameter{TViewModel, TParam}(RouteBase{TViewModel, TParam}, out TParam)"/>
    public bool TryGetRouteParameter<TViewModel, TParam>(RouteBase<TViewModel, TParam> route, [MaybeNullWhen(false)] out TParam parameter)
        where TViewModel : class, IRoutedViewModel<TParam>
        where TParam : notnull
    {
        EnsureThreadAccess();
        var routeItems = CurrentRouteInfo?.Items ?? [];

        for (int i = routeItems.Length - 1; i >= 0; i--)
        {
            var specifiedRoute = routeItems[i].SpecifiedRoute;

            if (specifiedRoute.Route == route && specifiedRoute is IParameterizedConcreteRoute<TViewModel, TParam> paramSpecifiedRoute)
            {
                parameter = paramSpecifiedRoute.Parameter;
                return true;
            }
        }

        parameter = default;
        return false;
    }

    /// <inheritdoc cref="INavigator.TryGetRouteViewModel{TViewModel}(out TViewModel)"/>
    public bool TryGetRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel)
        where TViewModel : class
    {
        EnsureThreadAccess();
        viewModel = CurrentRouteInfo?.Items.Select(ri => ri.ViewModel).OfType<TViewModel>().LastOrDefault();
        return viewModel is not null;
    }

    private class RouteInfo(ImmutableArray<RouteInfoItem> items, RouteOptions options)
    {
        public ImmutableArray<RouteInfoItem> Items { get; } = items;

        public RouteOptions Options { get; } = options;
    }

    private class RouteInfoItem(IConcreteRoute route, Func<UIElement> createViewFunc)
    {
        private readonly Func<UIElement> _createViewFunc = createViewFunc;

        public IConcreteRoute SpecifiedRoute { get; } = route;

        public UIElement? View { get; private set; }

        public IRoutedViewModelBase? ViewModel { get; private set; }

        public bool IsFirstNavigation { get; set; } = true;

        public bool AlreadyNavigatedTo { get; set; }

        public IViewNavigator? NestedViewNavigator => (View as IParentView)?.CreateNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized(VVMAction<object, object>? initializeViewHandler)
        {
            if (View is null)
            {
                View = _createViewFunc();
                ViewModel = ((IRoutedViewBase)View).Model ?? throw new InvalidOperationException($"View of type '{View.GetType()}' returned a null view model. View model must be created in the view's constructor.");
                SpecifiedRoute.Route.InitializeViewModel(ViewModel, SpecifiedRoute);
                initializeViewHandler?.Invoke(View, ViewModel);
            }

            Debug.Assert(ViewModel is not null, "View model should not be null after view is set.");
        }
    }
}
