using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Media;

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

    private event Action<Task>? AsyncNavigationTaskReceivers;

    private int _currentRouteIndex = -1;

    private bool _blockNavigation;
    private bool _blockDialogs;
    private CancellationTokenSource? _navigationCts;

    private RouteInfo? CurrentRouteInfo => _currentRouteIndex >= 0 && _currentRouteIndex < _routeInfoList.Count ? _routeInfoList[_currentRouteIndex] : null;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root content control and mappings provided in the build action.
    /// </summary>
    public Navigator(ContentControl rootContentControl, Action<NavigatorBuilder> buildAction)
        : this(new ContentControlNavigator(rootContentControl), buildAction) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root panel and mappings provided in the build action.
    /// </summary>
    public Navigator(Panel rootPanel, Action<NavigatorBuilder> buildAction) : this(new PanelNavigator(rootPanel), buildAction) { }

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

        _routes = [.. builder._routeList];
        _vmTypeToViewInfo = builder._vmTypeToViewInfo.ToFrozenDictionary();

        IEnumerable<KeyValuePair<Type, Func<ContentDialog>>> vmTypeToDialogCtor = builder._vmTypeToDialogCtor;

        if (!builder._vmTypeToDialogCtor.ContainsKey(typeof(MessageDialogViewModel)))
            vmTypeToDialogCtor = vmTypeToDialogCtor.Append(new(typeof(MessageDialogViewModel), () => new MessageDialog()));

        _vmTypeToDialogCtorFunc = vmTypeToDialogCtor.ToFrozenDictionary();
        _maxBackStackDepth = builder.MaxBackStackDepth;
    }

    /// <inheritdoc cref="INavigator.GetRouteOptions"/>
    public RouteOptions GetRouteOptions()
    {
        EnsureThreadAccess();
        return CurrentRouteInfo?.Options ?? RouteOptions.Empty;
    }

    /// <inheritdoc cref="INavigator.TryGetRouteParameter{TParam, TViewModel}(RouteBase{TParam, TViewModel}, out TParam)"/>
    public bool TryGetRouteParameter<TParam, TViewModel>(RouteBase<TParam, TViewModel> route, [MaybeNullWhen(false)] out TParam parameter)
        where TParam : notnull
        where TViewModel : class, IRoutedViewModel<TParam>
    {
        EnsureThreadAccess();
        var routeItems = CurrentRouteInfo?.Items ?? [];

        for (int i = routeItems.Length - 1; i >= 0; i--)
        {
            var specifiedRoute = routeItems[i].SpecifiedRoute;

            if (specifiedRoute.Route == route && specifiedRoute is IParameterizedSpecifiedRoute<TParam, TViewModel> paramSpecifiedRoute)
            {
                parameter = paramSpecifiedRoute.Parameter;
                return true;
            }
        }

        parameter = default;
        return false;
    }

    /// <inheritdoc cref="INavigator.TryGetRouteViewModel{TViewModel}(out TViewModel)"/>
    public bool TryGetRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel)
        where TViewModel : class
    {
        EnsureThreadAccess();
        viewModel = CurrentRouteInfo?.Items.Select(ri => ri.ViewModel).OfType<TViewModel>().LastOrDefault();
        return viewModel is not null;
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
