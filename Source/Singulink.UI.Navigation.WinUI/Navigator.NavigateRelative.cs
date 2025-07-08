namespace Singulink.UI.Navigation;

/// <content>
/// Provides relative navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.GoBackAsync(bool)"/>
    public Task<NavigationResult> GoBackAsync(bool userInitiated) => GoBackAsync(userInitiated, out _);

    /// <inheritdoc cref="INavigator.GoBackAsync(out bool)"/>
    public Task<NavigationResult> GoBackAsync(out bool handled) => GoBackAsync(true, out handled);

    private Task<NavigationResult> GoBackAsync(bool userInitiated, out bool handled)
    {
        EnsureThreadAccess();
        bool closedPopups = CloseLightDismissPopups();

        if (userInitiated)
        {
            if (closedPopups)
            {
                handled = true;
                return Task.FromResult(NavigationResult.Success);
            }

            if (!CanUserGoBack)
            {
                handled = false;
                return Task.FromResult(NavigationResult.Cancelled);
            }

            if (IsShowingDialog)
            {
                handled = true;

                var dialog = _dialogInfoStack.Peek().Dialog;
                ((IDismissableDialogViewModel)dialog.DataContext).OnDismissRequested();

                if (dialog == _dialogInfoStack.Peek().Dialog)
                    return Task.FromResult(NavigationResult.Cancelled);

                return _dialogInfoStack.Any(di => di.Dialog == dialog) ? Task.FromResult(NavigationResult.Rerouted) : Task.FromResult(NavigationResult.Success);
            }
        }

        handled = true;
        return GoBackAsync();

        async Task<NavigationResult> GoBackAsync() => await NavigateAsync(NavigationType.Back, null, null);
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
