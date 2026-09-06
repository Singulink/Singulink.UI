using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a presenter that can show dialogs. Implemented by both <see cref="INavigator"/> and <see cref="IDialogNavigator"/> (to show top-level and child
/// dialogs, respectively).
/// </summary>
public interface IDialogPresenter
{
    // TODO: Consider options for overloads that allow you to show a dialog with a parameterless constructor,
    // e.g. ShowDialogAsync<TViewModel>() where TViewModel : IDialogViewModel, new()

    /// <summary>
    /// Gets a value indicating whether this presenter can currently show a dialog. This is <see langword="false"/> for a navigator while any dialog is
    /// showing, for a dialog navigator whose dialog is not the top showing dialog, and for any presenter while showing dialogs is blocked (e.g. during
    /// navigation lifecycle methods that do not permit dialogs).
    /// </summary>
    public bool CanShowDialog { get; }

    /// <summary>
    /// Gets the root service provider. This property can be accessed from any thread.
    /// </summary>
    /// <remarks>
    /// This service provider is the root provider used to resolve services other than those provided directly by view models in a route.
    /// </remarks>
    public IServiceProvider RootServices { get; }

    /// <summary>
    /// Gets the task runner for this presenter. This property can be accessed from any thread.
    /// </summary>
    public ITaskRunner TaskRunner { get; }

    /// <summary>
    /// Creates a dialog view model with constructor injection, the same way the navigator creates routed view models. Explicit arguments are matched to
    /// constructor parameters by type (positionally among parameters of the same type); remaining parameters are resolved from child services registered
    /// by the dialogs this presenter is nested in and by the active route's view models (nearest first), then from <see cref="RootServices"/>, then from
    /// parameter default values. The view model's <c>Navigator</c> is available inside its constructor.
    /// </summary>
    /// <remarks>
    /// A dialog view model created by a presenter can only be shown by that presenter, since the services it received were resolved in that presenter's
    /// scope. If any service came from a view model that is no longer active when the dialog is shown, showing it throws. Create dialog view models
    /// immediately before showing them.
    /// </remarks>
    /// <typeparam name="TViewModel">The dialog view model type, which must have a single public constructor.</typeparam>
    /// <param name="explicitArgs">Values for constructor parameters that are not services (e.g. the item being edited). Must not contain nulls.</param>
    public TViewModel CreateDialogViewModel<[DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel>(params object?[] explicitArgs)
        where TViewModel : class, IDialogViewModel;

    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    public Task ShowDialogAsync(IDialogViewModel viewModel);

    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes with the dialog result when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel);
}
