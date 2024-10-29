using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <inheritdoc cref="IRoutedViewModel"/>
public abstract class RoutedViewModel : IRoutedViewModel
{
    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedToAsync"/>
    public virtual ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatingFromAsync"/>
    public virtual ValueTask OnNavigatingFromAsync(INavigator navigator, NavigatingArgs args) => ValueTask.CompletedTask;

    /// <inheritdoc cref="IRoutedViewModelBase.OnNavigatedFrom"/>
    public virtual void OnNavigatedFrom() { }
}

/// <inheritdoc cref="IRoutedViewModel{TParam}"/>
public abstract class RoutedViewModel<TParam> : IRoutedViewModel<TParam>
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
