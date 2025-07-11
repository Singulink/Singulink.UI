using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Contains information about a route.
/// </summary>
internal class ConcreteRoute : IConcreteRoute
{
    /// <inheritdoc cref="IConcreteRoute.Options"/>
    public RouteOptions Options { get; }

    /// <inheritdoc cref="IConcreteRoute.Parts"/>
    public IReadOnlyList<IConcreteRoutePart> Parts => field ??= Items.Select(i => i.ConcreteRoutePart).ToImmutableArray();

    internal ImmutableArray<Item> Items { get; }

    /// <inheritdoc cref="IConcreteRoute.Path"/>/>
    public string Path => field ??= Navigator.GetPath(Items.Select(i => i.ConcreteRoutePart));

    internal ConcreteRoute(ImmutableArray<Item> items, RouteOptions options)
    {
        Items = items;
        Options = options;
    }

    /// <inheritdoc cref="IConcreteRoute.ToString"/>/>
    public override string ToString() => $"{Path}{Options}";

    internal class Item(IConcreteRoutePart routePart, Func<UIElement> createViewFunc)
    {
        private readonly Func<UIElement> _createViewFunc = createViewFunc;

        public IConcreteRoutePart ConcreteRoutePart { get; } = routePart;

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

        public ViewNavigator? ChildViewNavigator => (View as IParentView)?.CreateChildViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized(INavigator navigator)
        {
            if (View is not null)
            {
                if (ViewModel != ((IRoutedViewBase)View).Model)
                    throw new InvalidOperationException($"View of type '{View.GetType()}' has a different view model than the one it was initialized with. Ensure the view model does not change after the view is created.");

                return;
            }

            View = _createViewFunc();
            ViewModel = ((IRoutedViewBase)View).Model ?? throw new InvalidOperationException($"View of type '{View.GetType()}' returned a null view model. View model must be created in the view's constructor.");
            ConcreteRoutePart.RoutePart.InitializeViewModel(ViewModel, navigator, ConcreteRoutePart);
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
            string result = ConcreteRoutePart.RoutePart.ViewModelType.Name.ToString();

            if (HasViewAndModel)
                result += "[cached]";

            result += $@", Path: ""{ConcreteRoutePart.ToString()}""";

            return result;
        }
    }
}
