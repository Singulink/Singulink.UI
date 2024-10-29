using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation.MvvmToolkit;

/// <summary>
/// Represents a view model with observable properties that can be navigated to with a parameterless route.
/// </summary>
public abstract class RoutedObservableViewModel : ObservableObject, IRoutedViewModel
{
    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedToAsync"/>
    public virtual ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatingFromAsync"/>
    public virtual ValueTask OnNavigatingFromAsync(INavigator navigator, NavigatingArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedFrom"/>
    public virtual void OnNavigatedFrom() { }
}

/// <summary>
/// Represents a view model with observable properties that can be navigated to with a parameterized route.
/// </summary>
public abstract class RoutedObservableViewModel<TParam> : ObservableObject, IRoutedViewModel<TParam>
    where TParam : notnull
{
    private RequiredSetOnce<TParam> _parameter;

    /// <inheritdoc cref="IRoutedViewModel{TParam}.Parameter"/>
    public TParam Parameter
    {
        get => _parameter.Value;
        set => _parameter.Value = value;
    }

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedToAsync"/>
    public virtual ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatingFromAsync"/>
    public virtual ValueTask OnNavigatingFromAsync(INavigator navigator, NavigatingArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedFrom"/>
    public virtual void OnNavigatedFrom() { }
}
