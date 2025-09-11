using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Contains information about a route.
/// </summary>
internal sealed class ConcreteRoute : IConcreteRoute
{
    /// <inheritdoc cref="IConcreteRoute.Options"/>
    public RouteOptions Options { get; }

    /// <inheritdoc cref="IConcreteRoute.Parts"/>
    public IReadOnlyList<IConcreteRoutePart> Parts => field ??= [.. Items.Select(i => i.ConcreteRoutePart)];

    internal IReadOnlyList<Item> Items { get; }

    /// <inheritdoc cref="IConcreteRoute.Path"/>
    public string Path => field ??= RoutingHelpers.GetPath(Items.Select(i => i.ConcreteRoutePart));

    internal ConcreteRoute(IReadOnlyList<Item> items, RouteOptions options)
    {
        Items = [..items];
        Options = options;
    }

    /// <inheritdoc cref="IConcreteRoute.ToString"/>
    public override string ToString() => $"{Path}{Options}";

    internal sealed class Item(Item? parentItem, IConcreteRoutePart concreteRoutePart, MappingInfo mappingInfo)
    {
        public IConcreteRoutePart ConcreteRoutePart => concreteRoutePart;

        public FrameworkElement? View { get; private set; }

        public IRoutedViewModelBase? ViewModel => (IRoutedViewModelBase)View?.DataContext;

        public Item? ParentItem => parentItem;

        public bool HasDependentChildren { get; private set; }

        [MemberNotNullWhen(true, nameof(View))]
        [MemberNotNullWhen(true, nameof(ViewModel))]
        public bool AlreadyNavigatedTo
        {
            get;
            set {
                if (value && !IsMaterialized)
                    throw new UnreachableException("Cannot set AlreadyNavigatedTo to true when the item is not materialized.");

                field = value;
            }
        }

        [MemberNotNullWhen(true, nameof(View))]
        [MemberNotNullWhen(true, nameof(ViewModel))]
        public bool IsMaterialized => View is not null;

        public ViewNavigator? ChildViewNavigator { get; private set; }

        public Type ViewModelType => mappingInfo.ViewModelType;

        public Type ViewType => mappingInfo.ViewType;

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureMaterialized(Navigator navigator)
        {
            if (IsMaterialized)
                return;

            var view = mappingInfo.CreateView();
            IRoutedViewModelBase viewModel;

            if (view.DataContext is null)
                view.DataContext = viewModel = mappingInfo.CreateViewModel(navigator, this);
            else if (view.DataContext.GetType().IsAssignableTo(mappingInfo.ViewModelType))
                viewModel = (IRoutedViewModelBase)view.DataContext;
            else
                throw new InvalidOperationException($"The view of type '{mappingInfo.ViewType}' has a data context of type '{view.DataContext.GetType()}' which is not assignable to the expected view model type '{mappingInfo.ViewModelType}'.");

            view.DataContextChanged += (s, e) => {
                if (e.NewValue != viewModel)
                {
                    view.DataContext = viewModel;
                    throw new InvalidOperationException("Navigator managed views cannot change their data context.");
                }
            };

            var childViewNavigator = (view as IParentView)?.CreateChildViewNavigator();

            View = view;
            ChildViewNavigator = childViewNavigator;

            Debug.Assert(ViewModel is not null, "View model should not be null");
        }

        public async ValueTask DisposeMaterializedComponents()
        {
            try
            {
                if (ViewModel is IAsyncDisposable asyncDisposableModel)
                    await asyncDisposableModel.DisposeAsync();
                else if (ViewModel is IDisposable disposableModel)
                    disposableModel.Dispose();
            }
            finally
            {
                View = null;
                ChildViewNavigator = null;

                AlreadyNavigatedTo = false;
                HasDependentChildren = false;
            }
        }

        public object? GetChildViewModelService(Type serviceType)
        {
            if (!IsMaterialized)
                throw new UnreachableException("Attempt to get services from a parent that is not materialized.");

            object service = (ViewModel as IServiceProvider)?.GetService(serviceType);

            if (service is not null)
            {
                if (!service.GetType().IsAssignableTo(serviceType))
                    throw new InvalidOperationException($"View model type '{ViewModel.GetType()}' returned a service type '{service.GetType()}' which is not assignable to requested service type '{serviceType}'.");
            }
            else if (ViewModel.GetType().IsAssignableTo(serviceType))
            {
                service = ViewModel;
            }

            if (service is not null)
            {
                HasDependentChildren = true;
                return service;
            }

            return ParentItem?.GetChildViewModelService(serviceType);
        }

        public override string ToString()
        {
            string result = ConcreteRoutePart.RoutePart.ViewModelType.Name;

            if (IsMaterialized)
                result += "[materialized]";

            result += $@", Path: ""{ConcreteRoutePart.ToString()}""";

            return result;
        }
    }
}
