using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.UI.Xaml.Media;
using Singulink.UI.Navigation.WinUI;

namespace Singulink.UI.Navigation;

/// <inheritdoc cref="INavigator"/>
public partial class Navigator : INavigator
{
    private readonly IViewNavigator _rootViewNavigator;

    private readonly FrozenDictionary<Type, ViewInfo> _vmTypeToViewInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _vmTypeToDialogCtorFunc;
    private readonly ImmutableArray<RouteBase> _routes;
    private readonly int _maxBackStackDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogInfoStack = [];
    private readonly List<RouteInfo> _routeInfoList = [];

    private int _currentRouteIndex = -1;

    private bool _blockNavigation;
    private bool _blockDialogs;
    private CancellationTokenSource? _navigationCts;

    private VVMAction<object, object>? _initializeViewHandler;
    private Action<Task>? _asyncNavigationHandler;

    private RouteInfo? CurrentRouteInfo => _currentRouteIndex >= 0 && _currentRouteIndex < _routeInfoList.Count ? _routeInfoList[_currentRouteIndex] : null;

    /// <inheritdoc cref="INavigator.CanUserGoBack"/>
    public bool CanUserGoBack
    {
        get {
            EnsureThreadAccess();

            if (IsNavigating)
                return false;

            if (IsShowingDialog)
                return _dialogInfoStack.Peek().Dialog.DataContext is IDismissableDialogViewModel;

            return HasBackHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanUserGoForward"/>
    public bool CanUserGoForward
    {
        get {
            EnsureThreadAccess();

            if (IsNavigating || IsShowingDialog)
                return false;

            return HasForwardHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanUserRefresh"/>
    public bool CanUserRefresh
    {
        get {
            EnsureThreadAccess();
            return !IsNavigating && !IsShowingDialog && CurrentRouteInfo is not null;
        }
    }

    /// <inheritdoc cref="INavigator.HasBackHistory"/>
    public bool HasBackHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex > 0;
        }
    }

    /// <inheritdoc cref="INavigator.HasForwardHistory"/>
    public bool HasForwardHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex < _routeInfoList.Count - 1;
        }
    }

    /// <inheritdoc cref="INavigator.DidNavigate"/>/>
    public bool DidNavigate => _currentRouteIndex >= 0;

    /// <inheritdoc cref="INavigator.IsNavigating"/>
    public bool IsNavigating
    {
        get {
            EnsureThreadAccess();
            return _blockNavigation || _navigationCts is not null;
        }
    }

    /// <inheritdoc cref="INavigator.IsShowingDialog"/>
    public bool IsShowingDialog
    {
        get {
            EnsureThreadAccess();
            return _dialogInfoStack.Count > 0;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root view navigator and mappings provided in the build action.
    /// </summary>
    public Navigator(IViewNavigator rootViewNavigator, Action<NavigatorBuilder> buildAction)
    {
        _rootViewNavigator = rootViewNavigator;
        EnsureThreadAccess();

        var builder = new NavigatorBuilder();
        buildAction(builder);
        builder.Validate();

        _routes = [.. builder.RouteList];
        _vmTypeToViewInfo = builder.VmTypeToViewInfo.ToFrozenDictionary();

        IEnumerable<KeyValuePair<Type, Func<ContentDialog>>> vmTypeToDialogCtor = builder.VmTypeToDialogCtor;

        if (!builder.VmTypeToDialogCtor.ContainsKey(typeof(MessageDialogViewModel)))
            vmTypeToDialogCtor = vmTypeToDialogCtor.Append(new(typeof(MessageDialogViewModel), () => new MessageDialog()));

        _vmTypeToDialogCtorFunc = vmTypeToDialogCtor.ToFrozenDictionary();
        _maxBackStackDepth = builder.MaxBackStackDepth;
    }

    /// <inheritdoc cref="INavigator.ClearHistory"/>
    public void ClearHistory()
    {
        EnsureThreadAccess();

        if (IsNavigating)
            throw new InvalidOperationException("Cannot clear history while navigating.");

        var currentRouteInfo = CurrentRouteInfo;

        if (currentRouteInfo is null)
            return; // Nothing to clear

        _routeInfoList.Clear();
        _routeInfoList.Add(currentRouteInfo);
        _currentRouteIndex = 0;
    }

    private void EnsureThreadAccess()
    {
        if (!_rootViewNavigator.DispatcherQueue.HasThreadAccess)
        {
            const string message = "Navigator members can only be accessed from the UI thread of the root view that this navigator is assigned to.";
            throw new InvalidOperationException(message);
        }
    }

    private bool CloseLightDismissPopups()
    {
        var xamlRoot = _rootViewNavigator.XamlRoot;
        bool closedPopup = false;

        if (xamlRoot is not null)
        {
            var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);

            foreach (var popup in popups.Where(p => p.IsLightDismissEnabled))
            {
                popup.IsOpen = false;
                closedPopup = true;
            }
        }

        return closedPopup;
    }
}
