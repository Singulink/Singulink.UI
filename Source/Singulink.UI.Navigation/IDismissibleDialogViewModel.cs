namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a dialog view model that can handle dismiss requests. If a dialog view model does not implement this interface, dismiss requests will be
/// ignored.
/// </summary>
/// <remarks>
/// A dismiss request is raised in the following situations:
/// <list type="bullet">
///   <item>A system back navigation is requested while the dialog is showing (e.g. via <see cref="INavigator.HandleSystemBackRequest"/>).</item>
///   <item>The close button is clicked (or activated by the escape key) and no command is wired to it. If a command is wired to the close button, the command
///     is invoked instead and no dismiss request is raised.</item>
/// </list>
/// Note that the escape key only triggers a close button click when the close button is visible (i.e. <c>CloseButtonText</c> is set to a non-empty
/// value). If the close button is hidden, escape key presses are ignored and will not raise a dismiss request, but system back requests will still raise one.
/// </remarks>
public interface IDismissibleDialogViewModel : IDialogViewModel
{
    /// <summary>
    /// Invoked when a dismiss request is raised while the dialog is showing. The implementation is responsible for closing the dialog (typically by calling
    /// <c>this.Navigator.Close()</c>) if the dismissal should proceed, or leaving the dialog open to veto the dismissal (optionally after prompting the
    /// user).
    /// </summary>
    public Task OnDismissRequestedAsync();
}

/// <summary>
/// Represents a dialog view model that can handle dismiss requests and produces a result when closed. See <see cref="IDismissibleDialogViewModel"/> for
/// details on when dismiss requests are raised.
/// </summary>
public interface IDismissibleDialogViewModel<TResult> : IDialogViewModel<TResult>, IDismissibleDialogViewModel;
