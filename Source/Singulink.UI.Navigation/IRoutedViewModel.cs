namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that is navigated to using a route part with no parameters.
/// </summary>
/// <remarks>
/// See <see cref="IRoutedViewModelBase"/> for available navigation members that can be implemented by view models.
/// </remarks>
public interface IRoutedViewModel : IRoutedViewModelBase
{
}

/// <summary>
/// Represents a view model that is navigated to using a parameterized route part.
/// </summary>
/// <typeparam name="TParam">The type of parameter (or parameters tuple, if there are multiple parameters).</typeparam>
/// <remarks>
/// See <see cref="IRoutedViewModelBase"/> for available navigation members that can be implemented by view models.
/// </remarks>
public interface IRoutedViewModel<TParam> : IRoutedViewModelBase where TParam : notnull
{
}
