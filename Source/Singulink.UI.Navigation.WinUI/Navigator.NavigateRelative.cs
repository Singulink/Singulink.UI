namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides relative navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.GoBackAsync()"/>
    public async Task<NavigationResult> GoBackAsync()
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateAsync(NavigationType.Back, null, null);
    }

    /// <inheritdoc cref="INavigator.HandleSystemBackRequest()"/>
    public bool HandleSystemBackRequest()
    {
        EnsureThreadAccess();

        if (CloseLightDismissPopups() || IsNavigating)
            return true;

        if (!CanUserGoBack)
            return false;

        if (IsShowingDialog)
        {
            var dialog = _dialogInfoStack.Peek().Dialog;
            ((IDismissableDialogViewModel)dialog.DataContext).OnDismissRequested();
        }
        else
        {
            TaskRunner.RunAndForget(false, NavigateAsync(NavigationType.Back, null, null));
        }

        return true;
    }

    /// <inheritdoc cref="INavigator.GoForwardAsync"/>
    public async Task<NavigationResult> GoForwardAsync(bool userInitiated)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        if (userInitiated && !CanUserGoForward)
            return NavigationResult.Cancelled;

        return await NavigateAsync(NavigationType.Forward, null, null);
    }

    /// <inheritdoc cref="INavigator.RefreshAsync"/>
    public async Task<NavigationResult> RefreshAsync(bool userInitiated)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        if (userInitiated && IsNavigating)
            return NavigationResult.Cancelled;

        return await NavigateAsync(NavigationType.Refresh, null, null);
    }
}
