using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.WinUI;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides the nested <see cref="RouteInfo"/> and <see cref="RouteInfoItem"/> classes.
/// </content>
partial class Navigator
{
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

        [MemberNotNullWhen(true, nameof(View))]
        [MemberNotNullWhen(true, nameof(ViewModel))]
        public bool AlreadyNavigatedTo
        {
            get;
            set {
                if (value && !HasViewAndModel)
                    throw new UnreachableException("Cannot set AlreadyNavigatedTo to true when the view and model have not been created yet.");

                field = value;
            }
        }

        [MemberNotNullWhen(true, nameof(View))]
        [MemberNotNullWhen(true, nameof(ViewModel))]
        public bool HasViewAndModel => View is not null && ViewModel is not null;

        public ViewNavigator? NestedViewNavigator => (View as IParentView)?.CreateNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized(INavigator navigator)
        {
            if (View is null)
            {
                View = _createViewFunc();
                ViewModel = ((IRoutedViewBase)View).Model ?? throw new InvalidOperationException($"View of type '{View.GetType()}' returned a null view model. View model must be created in the view's constructor.");
                SpecifiedRoute.Route.InitializeViewModel(ViewModel, navigator, SpecifiedRoute);
            }
            else if (ViewModel != ((IRoutedViewBase)View).Model)
            {
                throw new InvalidOperationException($"View of type '{View.GetType()}' has a different view model than the one it was initialized with. Ensure the view model does not change after the view is created.");
            }

            Debug.Assert(ViewModel is not null, "View model should not be null after view is set.");
        }

        public void ClearViewAndModel()
        {
            View = null;
            ViewModel = null;
            IsFirstNavigation = true;
            AlreadyNavigatedTo = false;
        }

        public override string ToString()
        {
            string result = SpecifiedRoute.Route.ViewModelType.Name.ToString();

            if (HasViewAndModel)
                result += " (cached)";

            result += $@", Route: ""{SpecifiedRoute.ToString()}""";

            return result;
        }
    }
}
