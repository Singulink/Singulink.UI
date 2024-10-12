using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation.MvvmToolkit;

/// <summary>
/// Represents a view model with observable properties that can act as a recipient for messages and be navigated to with a parameterless route.
/// </summary>
public abstract class RoutedObservableRecipientViewModel : ObservableRecipient, IRoutedViewModel
{
    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedToAsync"/>
    public virtual ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatingFromAsync"/>
    public virtual ValueTask OnNavigatingFromAsync(INavigator navigator, NavigatingArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedFrom"/>
    public virtual void OnNavigatedFrom() { }
}

/// <summary>
/// Represents a view model with observable properties that can act as a recipient for messages and can be navigated to with a parameterized route.
/// </summary>
public abstract class RoutedObservableRecipientViewModel<TParam> : ObservableRecipient, IRoutedViewModel<TParam>
    where TParam : notnull
{
    private RequiredSingleSet<TParam> _parameter;

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
