using System.ComponentModel;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides INotifyPropertyChanged related implementations for the navigator.
/// </content>
partial class NavigatorCore
{
    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

    /// <summary>
    /// Tracks navigator property state at the start of an operation and raises change notifications on <see cref="Update"/> or disposal.
    /// </summary>
    private protected sealed partial class PropertyChangedNotifier(NavigatorCore navigator) : IDisposable
    {
        private static readonly PropertyChangedEventArgs CanUserGoBackChangedArgs = new(nameof(CanGoBack));
        private static readonly PropertyChangedEventArgs CanUserGoForwardChangedArgs = new(nameof(CanGoForward));
        private static readonly PropertyChangedEventArgs CanUserRefreshChangedArgs = new(nameof(CanRefresh));
        private static readonly PropertyChangedEventArgs HasBackHistoryChangedArgs = new(nameof(HasBackHistory));
        private static readonly PropertyChangedEventArgs HasForwardHistoryChangedArgs = new(nameof(HasForwardHistory));
        private static readonly PropertyChangedEventArgs IsNavigatingChangedArgs = new(nameof(IsNavigating));
        private static readonly PropertyChangedEventArgs IsShowingDialogChangedArgs = new(nameof(IsShowingDialog));
        private static readonly PropertyChangedEventArgs CurrentRouteChangedArgs = new(nameof(CurrentRoute));

        private NavigatorCore? _navigator = navigator;

        private NavigatorRoute? _currentRoute = navigator.CurrentRouteCore;
        private bool _canGoBack = navigator.CanGoBack;
        private bool _canGoForward = navigator.CanGoForward;
        private bool _canRefresh = navigator.CanRefresh;
        private bool _hasBackHistory = navigator.HasBackHistory;
        private bool _hasForwardHistory = navigator.HasForwardHistory;
        private bool _isNavigating = navigator.IsNavigating;
        private bool _isShowingDialog = navigator.IsShowingDialog;

        /// <summary>
        /// Checks whether any tracked property values have changed and raises the appropriate property changed events.
        /// </summary>
        public void Update()
        {
            ObjectDisposedException.ThrowIf(_navigator is null, this);

            CheckUpdateNotify(ref _canGoBack, _navigator.CanGoBack, CanUserGoBackChangedArgs);
            CheckUpdateNotify(ref _canGoForward, _navigator.CanGoForward, CanUserGoForwardChangedArgs);
            CheckUpdateNotify(ref _canRefresh, _navigator.CanRefresh, CanUserRefreshChangedArgs);
            CheckUpdateNotify(ref _hasBackHistory, _navigator.HasBackHistory, HasBackHistoryChangedArgs);
            CheckUpdateNotify(ref _hasForwardHistory, _navigator.HasForwardHistory, HasForwardHistoryChangedArgs);
            CheckUpdateNotify(ref _isNavigating, _navigator.IsNavigating, IsNavigatingChangedArgs);
            CheckUpdateNotify(ref _isShowingDialog, _navigator.IsShowingDialog, IsShowingDialogChangedArgs);

            if (CheckUpdateNotify(ref _currentRoute, _navigator.CurrentRouteCore, CurrentRouteChangedArgs) && _currentRoute is { } route)
            {
                _navigator.OnCurrentRouteChanged(route);
            }

            bool CheckUpdateNotify<T>(ref T field, T value, PropertyChangedEventArgs e)
            {
                if (!EqualityComparer<T>.Default.Equals(field, value))
                {
                    field = value;
                    _navigator?.OnPropertyChanged(e);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Calls <see cref="Update"/> and releases the notifier.
        /// </summary>
        public void Dispose()
        {
            if (_navigator is not null)
            {
                Update();
                _navigator = null;
            }
        }
    }
}
