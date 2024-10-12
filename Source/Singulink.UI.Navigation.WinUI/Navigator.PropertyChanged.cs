using System.ComponentModel;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides INotifyPropertyChanged related implementations for the navigator.
/// </content>
public partial class Navigator
{
    private static readonly PropertyChangedEventArgs IsNavigatingChangedArgs = new(nameof(IsNavigating));
    private static readonly PropertyChangedEventArgs IsShowingDialogChangedArgs = new(nameof(IsShowingDialog));
    private static readonly PropertyChangedEventArgs HasBackHistoryChangedArgs = new(nameof(HasBackHistory));
    private static readonly PropertyChangedEventArgs HasForwardHistoryChangedArgs = new(nameof(HasForwardHistory));
    private static readonly PropertyChangedEventArgs CanUserGoBackChangedArgs = new(nameof(CanUserGoBack));
    private static readonly PropertyChangedEventArgs CanUserGoForwardChangedArgs = new(nameof(CanUserGoForward));
    private static readonly PropertyChangedEventArgs CanUserRefreshChangedArgs = new(nameof(CanUserRefresh));

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

    private struct PropertyChangedNotifier : IDisposable
    {
        private INavigator? _navigator;
        private readonly Action<PropertyChangedEventArgs> _onPropertyChanged;

        private bool _isNavigating;
        private bool _isShowingDialog;
        private bool _hasBackHistory;
        private bool _hasForwardHistory;
        private bool _canUserGoBack;
        private bool _canUserGoForward;
        private bool _canUserRefresh;

        public PropertyChangedNotifier(INavigator navigator, Action<PropertyChangedEventArgs> onPropertyChanged)
        {
            _navigator = navigator;
            _onPropertyChanged = onPropertyChanged;

            _isNavigating = navigator.IsNavigating;
            _isShowingDialog = navigator.IsShowingDialog;
            _hasBackHistory = navigator.HasBackHistory;
            _hasForwardHistory = navigator.HasForwardHistory;
            _canUserGoBack = navigator.CanUserGoBack;
            _canUserGoForward = navigator.CanUserGoForward;
            _canUserRefresh = navigator.CanUserRefresh;
        }

        public void Update()
        {
            if (_navigator is null)
                throw new ObjectDisposedException(typeof(PropertyChangedNotifier).Name);

            CheckUpdateNotify(ref _isNavigating, _navigator.IsNavigating, IsNavigatingChangedArgs);
            CheckUpdateNotify(ref _isShowingDialog, _navigator.IsShowingDialog, IsShowingDialogChangedArgs);
            CheckUpdateNotify(ref _hasBackHistory, _navigator.HasBackHistory, HasBackHistoryChangedArgs);
            CheckUpdateNotify(ref _hasForwardHistory, _navigator.HasForwardHistory, HasForwardHistoryChangedArgs);
            CheckUpdateNotify(ref _canUserGoBack, _navigator.CanUserGoBack, CanUserGoBackChangedArgs);
            CheckUpdateNotify(ref _canUserGoForward, _navigator.CanUserGoForward, CanUserGoForwardChangedArgs);
            CheckUpdateNotify(ref _canUserRefresh, _navigator.CanUserRefresh, CanUserRefreshChangedArgs);
        }

        public void Dispose()
        {
            if (_navigator is not null)
            {
                Update();
                _navigator = null;
            }
        }

        private void CheckUpdateNotify(ref bool field, bool value, PropertyChangedEventArgs e)
        {
            if (field != value)
            {
                field = value;
                _onPropertyChanged(e);
            }
        }
    }
}
