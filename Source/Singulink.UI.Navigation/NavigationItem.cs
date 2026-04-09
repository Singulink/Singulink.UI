using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

internal sealed class NavigationItem
{
    internal NavigationItem(NavigationItem? parentItem, IConcreteRoutePart concreteRoutePart, Type viewModelType)
    {
        ParentItem = parentItem;
        ConcreteRoutePart = concreteRoutePart;
        ViewModelType = viewModelType;
    }

    internal IConcreteRoutePart ConcreteRoutePart { get; set; }

    internal NavigationItem? ParentItem { get; }

    internal Type ViewModelType { get; }

    internal IRoutedViewModelBase? ViewModel { get; set; }

    internal object? View { get; set; }

    internal object? ChildViewNavigator { get; set; }

    internal bool HasDependentChildren { get; set; }

    [MemberNotNullWhen(true, nameof(ViewModel))]
    internal bool AlreadyNavigatedTo
    {
        get;
        set {
            if (value && !IsMaterialized)
                throw new UnreachableException("Cannot set AlreadyNavigatedTo to true when the item is not materialized.");

            field = value;
        }
    }

    [MemberNotNullWhen(true, nameof(ViewModel))]
    internal bool IsMaterialized => ViewModel is not null;

    internal object? GetChildViewModelService(Type serviceType)
    {
        if (!IsMaterialized)
            throw new UnreachableException("Attempt to get services from a parent that is not materialized.");

        object? service = MixinManager.GetChildService(ViewModel, serviceType) ??
            (ViewModel as IServiceProvider)?.GetService(serviceType);

        if (service is not null)
        {
            if (!service.GetType().IsAssignableTo(serviceType))
            {
                throw new InvalidOperationException(
                    $"View model type '{ViewModel.GetType()}' returned a service type '{service.GetType()}' " +
                    $"which is not assignable to requested service type '{serviceType}'.");
            }
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

    internal async ValueTask DisposeMaterializedComponents()
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
            ViewModel = null;
            View = null;
            ChildViewNavigator = null;
            AlreadyNavigatedTo = false;
            HasDependentChildren = false;
        }
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
