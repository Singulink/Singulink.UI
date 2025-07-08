namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to with a parameterless route.
/// </summary>
public interface IRoutedViewModel : IRoutedViewModelBase { }

/// <summary>
/// Represents a view model that can be navigated to with a parameterized route.
/// </summary>
/// <typeparam name="TParam">The type of the parameter (or a tuple of parameters, if there are multiple parameters).</typeparam>
public interface IRoutedViewModel<TParam> : IRoutedViewModelBase where TParam : notnull
{
    /// <summary>
    /// Gets or sets the route parameter (or a tuple of parameters, if there are multiple parameters) for this view model. This property is set once prior to any
    /// navigation events being called on the view model. Getting the value before it is set or setting it more than once will throw an <see
    /// cref="InvalidOperationException"/>.
    /// </summary>
    public TParam Parameter { get; set; }
}
