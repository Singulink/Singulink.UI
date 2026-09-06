using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// Low-level helpers for associating navigators and parameters with view models directly, for mock-style tests that do not use
/// <see cref="TestNavigator"/>. Each association can only be made once per view model instance, matching the application navigator.
/// </summary>
/// <remarks>
/// Prefer <see cref="TestNavigator"/> for routed view models: it creates them the same way the application does (with the navigator and parameter
/// available inside the constructor) and runs the real navigation engine. These helpers attach to an already constructed instance, so a view model that
/// uses its navigator or parameter in its constructor cannot be set up this way.
/// </remarks>
public static class ViewModelTestSupport
{
    /// <summary>
    /// Associates the specified navigator with a dialog view model so that its <c>Navigator</c> and <c>TaskRunner</c> extension properties return it.
    /// </summary>
    /// <exception cref="InvalidOperationException">A navigator has already been associated with the view model.</exception>
    public static void AttachNavigator(this IDialogViewModel viewModel, IDialogNavigator navigator)
    {
        MixinManager.SetNavigator(viewModel, navigator);
    }

    /// <summary>
    /// Associates the specified navigator with a routed view model so that its <c>Navigator</c> and <c>TaskRunner</c> extension properties return it.
    /// </summary>
    /// <exception cref="InvalidOperationException">A navigator has already been associated with the view model.</exception>
    public static void AttachNavigator(this IRoutedViewModelBase viewModel, INavigator navigator)
    {
        MixinManager.SetNavigator(viewModel, navigator);
    }

    /// <summary>
    /// Associates the specified route parameter with a routed view model so that its <c>Parameter</c> extension property returns it.
    /// </summary>
    /// <exception cref="InvalidOperationException">A parameter has already been associated with the view model.</exception>
    public static void SetParameter<TParam>(this IRoutedViewModel<TParam> viewModel, TParam parameter) where TParam : notnull
    {
        MixinManager.SetParameter(viewModel, parameter);
    }
}
