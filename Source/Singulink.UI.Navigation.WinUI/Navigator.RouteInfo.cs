using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides ....
/// </content>
public partial class Navigator
{
    private class RouteInfo(ImmutableArray<RouteInfoItem> items, RouteOptions options)
    {
        public ImmutableArray<RouteInfoItem> Items { get; } = items;

        public RouteOptions Options { get; } = options;
    }

    private class RouteInfoItem(ISpecifiedRoute route, Func<UIElement> createViewFunc)
    {
        private readonly Func<UIElement> _createViewFunc = createViewFunc;

        public ISpecifiedRoute SpecifiedRoute { get; } = route;

        public UIElement? View { get; private set; }

        public IRoutedViewModelBase? ViewModel => (View as IRoutedView)?.Model;

        public bool IsFirstNavigation { get; set; } = true;

        public bool AlreadyNavigatedTo { get; set; }

        public IViewNavigator? NestedViewNavigator => (View as IParentView)?.CreateNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized()
        {
            View ??= _createViewFunc();
            Debug.Assert(ViewModel is not null, "View model should not be null after view is created.");
            SpecifiedRoute.Route.InitializeViewModel(ViewModel, SpecifiedRoute);
        }
    }
}
