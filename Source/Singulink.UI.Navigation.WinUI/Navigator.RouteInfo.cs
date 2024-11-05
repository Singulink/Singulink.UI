using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides the nested <see cref="RouteInfo"/> and <see cref="RouteInfoItem"/> classes.
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

        public IRoutedViewModelBase? ViewModel { get; private set; }

        public bool IsFirstNavigation { get; set; } = true;

        public bool AlreadyNavigatedTo { get; set; }

        public IViewNavigator? NestedViewNavigator => (View as IParentView)?.CreateNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized()
        {
            if (View is null)
            {
                View = _createViewFunc();
                ViewModel = ((IRoutedView)View).Model ?? throw new InvalidOperationException($"View of type '{View.GetType()}' returned a null view model. View model must be created in the view's constructor.");
                SpecifiedRoute.Route.InitializeViewModel(ViewModel, SpecifiedRoute);
            }

            Debug.Assert(ViewModel is not null, "View model should not be null after view is set.");
        }
    }
}
