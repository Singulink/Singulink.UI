using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to with a parameterless route.
/// </summary>
/// <remarks>
/// See <see cref="IRoutedViewModelBase"/> for available navigation methods and properties that can be implemented by view models.
/// </remarks>
public interface IRoutedViewModel : IRoutedViewModelBase
{
}

/// <summary>
/// Represents a view model that can be navigated to with a parameterized route.
/// </summary>
/// <typeparam name="TParam">The type of the parameter (or a tuple of parameters, if there are multiple parameters).</typeparam>
/// <remarks>
/// See <see cref="IRoutedViewModelBase"/> for available navigation methods and properties that can be implemented by view models.
/// </remarks>
public interface IRoutedViewModel<TParam> : IRoutedViewModelBase where TParam : notnull
{
}
