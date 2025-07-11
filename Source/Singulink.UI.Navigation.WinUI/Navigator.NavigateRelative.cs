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

    /// <inheritdoc cref="INavigator.GoForwardAsync"/>
    public async Task<NavigationResult> GoForwardAsync()
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateAsync(NavigationType.Forward, null, null);
    }

    /// <inheritdoc cref="INavigator.HandleSystemBackRequest()"/>
    public bool HandleSystemBackRequest()
    {
        EnsureThreadAccess();

        if (CloseLightDismissPopups() || IsNavigating)
            return true;

        if (IsShowingDialog)
        {
            if (_dialogTcsStack.Peek().Dialog.DataContext is IDismissableDialogViewModel dismissableVm)
                dismissableVm.OnDismissRequested();

            return true;
        }

        if (!HasBackHistory)
            return false;

        TaskRunner.RunAndForget(NavigateAsync(NavigationType.Back, null, null));
        return true;
    }

    /// <inheritdoc cref="INavigator.HandleSystemForwardRequest()"/>
    public bool HandleSystemForwardRequest()
    {
        EnsureThreadAccess();

        if (IsShowingDialog || IsNavigating)
            return true;

        if (!HasForwardHistory)
            return false;

        CloseLightDismissPopups();
        TaskRunner.RunAndForget(NavigateAsync(NavigationType.Forward, null, null));
        return true;
    }

    /// <inheritdoc cref="INavigator.RefreshAsync"/>
    public async Task<NavigationResult> RefreshAsync()
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateAsync(NavigationType.Refresh, null, null);
    }
}
