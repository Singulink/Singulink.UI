using Singulink.UI.Navigation.InternalServices;

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

        if (!HasBackHistory)
            throw new InvalidOperationException("Cannot navigate back when the back history is empty.");

        var route = _routeStack[_currentRouteIndex - 1];

        return await NavigateAsyncCore(NavigationType.Back, route, () => {
            _currentRouteIndex--;
            return null;
        });
    }

    /// <inheritdoc cref="INavigator.GoForwardAsync"/>
    public async Task<NavigationResult> GoForwardAsync()
    {
        EnsureThreadAccess();

        if (!HasForwardHistory)
            throw new InvalidOperationException("Cannot navigate forward when the forward history is empty.");

        var route = _routeStack[_currentRouteIndex + 1];

        return await NavigateAsyncCore(NavigationType.Forward, route, () => {
            _currentRouteIndex++;
            return null;
        });
    }

    /// <inheritdoc cref="INavigator.HandleSystemBackRequest()"/>
    public bool HandleSystemBackRequest()
    {
        EnsureThreadAccess();

        if (CloseLightDismissPopups() || IsNavigating)
            return true;

        if (_dialogStack.TryPeek(out var dialogInfo))
        {
            if (dialogInfo.Dialog.DataContext is IDismissibleDialogViewModel dismissibleVm &&
                MixinManager.GetNavigator(dismissibleVm) is { } dn && !dn.TaskRunner.IsBusy)
            {
                dn.TaskRunner.RunAsBusyAndForget(dismissibleVm.OnDismissRequestedAsync());
            }

            return true;
        }

        if (!HasBackHistory)
            return false;

        TaskRunner.RunAndForget(GoBackAsync());

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

        TaskRunner.RunAndForget(GoForwardAsync());

        return true;
    }

    /// <inheritdoc cref="INavigator.RefreshAsync"/>
    public async Task<NavigationResult> RefreshAsync()
    {
        EnsureThreadAccess();

        var route = CurrentRouteImpl ?? throw new InvalidOperationException("Cannot refresh before the navigator has a route.");
        return await NavigateAsyncCore(NavigationType.Refresh, route, null);
    }
}
