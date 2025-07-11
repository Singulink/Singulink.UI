using System.ComponentModel;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides INotifyPropertyChanged related implementations for the navigator.
/// </content>
partial class Navigator
{
    private static readonly PropertyChangedEventArgs CanUserGoBackChangedArgs = new(nameof(CanGoBack));
    private static readonly PropertyChangedEventArgs CanUserGoForwardChangedArgs = new(nameof(CanGoForward));
    private static readonly PropertyChangedEventArgs CanUserRefreshChangedArgs = new(nameof(CanRefresh));
    private static readonly PropertyChangedEventArgs HasBackHistoryChangedArgs = new(nameof(HasBackHistory));
    private static readonly PropertyChangedEventArgs HasForwardHistoryChangedArgs = new(nameof(HasForwardHistory));
    private static readonly PropertyChangedEventArgs IsNavigatingChangedArgs = new(nameof(IsNavigating));
    private static readonly PropertyChangedEventArgs IsShowingDialogChangedArgs = new(nameof(IsShowingDialog));
    private static readonly PropertyChangedEventArgs CurrentRouteChangedArgs = new(nameof(CurrentRoute));

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

    private struct PropertyChangedNotifier : IDisposable
    {
        private Navigator? _navigator;
        private readonly Action<PropertyChangedEventArgs> _onPropertyChanged;

        private bool _canUserGoBack;
        private bool _canUserGoForward;
        private bool _canUserRefresh;
        private bool _hasBackHistory;
        private bool _hasForwardHistory;
        private bool _isNavigating;
        private bool _isShowingDialog;
        private ConcreteRoute? _currentRoute;

        public PropertyChangedNotifier(Navigator navigator, Action<PropertyChangedEventArgs> onPropertyChanged)
        {
            _navigator = navigator;
            _onPropertyChanged = onPropertyChanged;

            _canUserGoBack = navigator.CanGoBack;
            _canUserGoForward = navigator.CanGoForward;
            _canUserRefresh = navigator.CanRefresh;
            _hasBackHistory = navigator.HasBackHistory;
            _hasForwardHistory = navigator.HasForwardHistory;
            _isNavigating = navigator.IsNavigating;
            _isShowingDialog = navigator.IsShowingDialog;
            _currentRoute = navigator.CurrentRouteInternal;
        }

        public void Update()
        {
            if (_navigator is null)
                throw new ObjectDisposedException(typeof(PropertyChangedNotifier).Name);

            CheckUpdateNotify(ref _canUserGoBack, _navigator.CanGoBack, CanUserGoBackChangedArgs);
            CheckUpdateNotify(ref _canUserGoForward, _navigator.CanGoForward, CanUserGoForwardChangedArgs);
            CheckUpdateNotify(ref _canUserRefresh, _navigator.CanRefresh, CanUserRefreshChangedArgs);
            CheckUpdateNotify(ref _hasBackHistory, _navigator.HasBackHistory, HasBackHistoryChangedArgs);
            CheckUpdateNotify(ref _hasForwardHistory, _navigator.HasForwardHistory, HasForwardHistoryChangedArgs);
            CheckUpdateNotify(ref _isNavigating, _navigator.IsNavigating, IsNavigatingChangedArgs);
            CheckUpdateNotify(ref _isShowingDialog, _navigator.IsShowingDialog, IsShowingDialogChangedArgs);
            CheckUpdateNotify(ref _currentRoute, _navigator.CurrentRouteInternal, CurrentRouteChangedArgs);
        }

        public void Dispose()
        {
            if (_navigator is not null)
            {
                Update();
                _navigator = null;
            }
        }

        private void CheckUpdateNotify<T>(ref T field, T value, PropertyChangedEventArgs e)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                _onPropertyChanged(e);
            }
        }
    }
}
