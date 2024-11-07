using Microsoft.UI.Xaml.Media;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides relative navigation related implementations for the navigator.
/// </content>
public partial class Navigator
{
    /// <inheritdoc cref="INavigator.GoBackAsync"/>
    public async Task<NavigationResult> GoBackAsync(bool userInitiated)
    {
        EnsureThreadAccess();
        bool closedPopups = CloseLightDismissPopups();

        if (userInitiated)
        {
            if (closedPopups)
                return NavigationResult.Success;

            if (!CanUserGoBack)
                return NavigationResult.Cancelled;

            if (IsShowingDialog)
            {
                var dialog = _dialogInfoStack.Peek().Dialog;
                ((IDismissableDialogViewModel)dialog.DataContext).OnDismissRequested();

                if (dialog == _dialogInfoStack.Peek().Dialog)
                    return NavigationResult.Cancelled;

                return _dialogInfoStack.Any(di => di.Dialog == dialog) ? NavigationResult.Rerouted : NavigationResult.Success;
            }
        }

        return await NavigateAsync(NavigationType.Back, null, null);
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

    private bool CloseLightDismissPopups()
    {
        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(_rootViewNavigator.XamlRoot);
        bool closedPopup = false;

        foreach (var f in popups.Reverse().TakeWhile(f => f.IsLightDismissEnabled))
        {
            f.IsOpen = false;
            closedPopup = true;
        }

        return closedPopup;
    }
}
