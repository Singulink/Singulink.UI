using Singulink.UI.Navigation.InternalServices;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

#pragma warning disable CA1708 // Identifiers should differ by more than case: https://github.com/dotnet/roslyn/issues/78859

/// <summary>
/// Provides extension methods for view models to access their associated navigators and parameters.
/// </summary>
public static class ViewModelExtensions
{
    /// <summary>
    /// Provides extension methods for routed view models without parameters.
    /// </summary>
    extension(IRoutedViewModelBase viewModel)
    {
        /// <summary>
        /// Gets the navigator for the view model.
        /// </summary>
        public INavigator Navigator => MixinManager.GetNavigator(viewModel) ??
            throw new InvalidOperationException("View model is not associated with a navigator.");

        /// <summary>
        /// Gets the task runner for the view model.
        /// </summary>
        public ITaskRunner TaskRunner => viewModel.Navigator.TaskRunner;
    }

    /// <summary>
    /// Provides extension methods for routed view models with parameters.
    /// </summary>
    extension<TParam>(IRoutedViewModel<TParam> viewModel) where TParam : notnull
    {
        /// <summary>
        /// Gets the parameter (or parameters tuple, if there are multiple parameters) for the view model.
        /// </summary>
        public TParam Parameter => MixinManager.TryGetParameter(viewModel, out var parameter) ? parameter :
            throw new InvalidOperationException("View model is not associated with a parameter.");
    }

    /// <summary>
    /// Provides extension methods for dialog view models to access their associated dialog navigators.
    /// </summary>
    extension(IDialogViewModel viewModel)
    {
        /// <summary>
        /// Gets the dialog navigator for the view model.
        /// </summary>
        public IDialogNavigator Navigator => MixinManager.GetNavigator(viewModel) ??
                throw new InvalidOperationException("View model is not associated with a dialog navigator.");

        /// <summary>
        /// Gets the task runner for the view model.
        /// </summary>
        public ITaskRunner TaskRunner => viewModel.Navigator.TaskRunner;
    }
}
