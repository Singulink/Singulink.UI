using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides extension methods for view models to access their associated navigators and parameters.
/// </summary>
public static class ViewModelExtensions
{
    /// <summary>
    /// Returns the dialog navigator for the view model.
    /// </summary>
    public static IDialogNavigator GetNavigator(this IDialogViewModel viewModel)
    {
        return MixinManager.GetDialogNavigator(viewModel) ??
            throw new InvalidOperationException("No dialog navigator has been set. Ensure the dialog has been shown.");
    }

    /// <summary>
    /// Returns the navigator for the view model.
    /// </summary>
    public static INavigator GetNavigator(this IRoutedViewModelBase viewModel)
    {
        return MixinManager.GetNavigator(viewModel) ??
            throw new InvalidOperationException("No navigator has been set. Ensure the view model has been navigated to.");
    }

    /// <summary>
    /// Returns the parameter (or parameters tuple, if there are multiple parameters) for the view model.
    /// </summary>
    public static TParam GetParameter<TParam>(this IRoutedViewModel<TParam> viewModel) where TParam : notnull
    {
        if (!MixinManager.TryGetParameter(viewModel, out var parameter))
            throw new InvalidOperationException("No parameter has been set. Ensure the view model has been navigated to.");

        return parameter;
    }

    /// <summary>
    /// Returns a value indicating whether the view model has been navigated to and has an accessible navigator.
    /// </summary>
    public static bool HasNavigated(this IRoutedViewModelBase viewModel)
    {
        return MixinManager.GetNavigator(viewModel) is not null;
    }
}
