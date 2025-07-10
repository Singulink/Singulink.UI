using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Media;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

/// <inheritdoc cref="INavigator"/>
public partial class Navigator : INavigator
{
    private readonly ViewNavigator _viewNavigator;

    private readonly FrozenDictionary<Type, ViewInfo> _vmTypeToViewInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _vmTypeToDialogActivator;
    private readonly ImmutableArray<RouteBase> _routes;
    private readonly int _maxStackSize;
    private readonly int _maxBackStackCachedViewDepth;
    private readonly int _maxForwardStackCachedViewDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogInfoStack = [];
    private readonly List<RouteInfo> _routeInfoList = [];

    private int _currentRouteIndex = -1;

    private bool _blockNavigation;
    private bool _blockDialogs;
    private CancellationTokenSource? _navigationCts;

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

    /// <inheritdoc cref="INavigator.CurrentRoute"/>
    public string? CurrentRoute
    {
        get {
            var currentRouteInfo = CurrentRouteInfo;

            if (currentRouteInfo is null)
                return null;

            var routes = currentRouteInfo.Items.Select(ri => ri.SpecifiedRoute);
            return GetRouteString(routes);
        }
    }

    /// <inheritdoc cref="INavigator.TaskRunner"/>/>
    public ITaskRunner TaskRunner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class using the specified content control for displaying the active view and mappings provided
    /// in the build action.
    /// </summary>
    public Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)
        : this(new ContentControlNavigator(contentControl), buildAction) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root view navigator and mappings provided in the build action.
    /// </summary>
    public Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)
    {
        _viewNavigator = viewNavigator;
        EnsureThreadAccess();
        TaskRunner = new TaskRunner(busy => _viewNavigator.NavControl.IsEnabled = !busy);

        var builder = new NavigatorBuilder();
        buildAction(builder);
        builder.Validate();

        _routes = [.. builder.RouteList];
        _vmTypeToViewInfo = builder.VmTypeToViewInfo.ToFrozenDictionary();

        IEnumerable<KeyValuePair<Type, Func<ContentDialog>>> vmTypeToDialogActivator = builder.VmTypeToDialogActivator;

        if (!builder.VmTypeToDialogActivator.ContainsKey(typeof(MessageDialogViewModel)))
            vmTypeToDialogActivator = vmTypeToDialogActivator.Append(new(typeof(MessageDialogViewModel), () => new MessageDialog()));

        _vmTypeToDialogActivator = vmTypeToDialogActivator.ToFrozenDictionary();
        _maxStackSize = builder.MaxNavigationStacksSize;
        _maxBackStackCachedViewDepth = builder.MaxBackStackCachedViewDepth;
        _maxForwardStackCachedViewDepth = builder.MaxForwardStackCachedViewDepth;
    }

    /// <inheritdoc cref="INavigator.ClearHistory"/>
    public void ClearHistory()
    {
        EnsureThreadAccess();

        var currentRouteInfo = CurrentRouteInfo;

        if (currentRouteInfo is null)
            return; // Nothing to clear

        using (new PropertyChangedNotifier(this, OnPropertyChanged))
        {
            _routeInfoList.Clear();
            _routeInfoList.Add(currentRouteInfo);
            _currentRouteIndex = 0;
        }
    }

    private void EnsureThreadAccess()
    {
        if (_viewNavigator.NavControl.DispatcherQueue?.HasThreadAccess is not true)
        {
            const string message = "Navigator can only be accessed from the UI thread.";
            throw new InvalidOperationException(message);
        }
    }

    private bool CloseLightDismissPopups()
    {
        var xamlRoot = _viewNavigator.NavControl.XamlRoot;
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

    /// <inheritdoc cref="INavigator.GetBackStackRoutes"/>
    public IList<string> GetBackStackRoutes()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex <= 0)
            return [];

        var routeInfoList = new List<string>(_currentRouteIndex);

        for (int i = _currentRouteIndex - 1; i >= 0; i--)
        {
            var routes = _routeInfoList[i].Items.Select(ri => ri.SpecifiedRoute);
            routeInfoList.Add(GetRouteString(routes));
        }

        return routeInfoList;
    }

    /// <inheritdoc cref="INavigator.GetForwardStackRoutes"/>
    public IList<string> GetForwardStackRoutes()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex < 0 || _currentRouteIndex >= _routeInfoList.Count - 1)
            return [];

        var routeInfoList = new List<string>(_routeInfoList.Count - _currentRouteIndex - 1);

        for (int i = _currentRouteIndex + 1; i < _routeInfoList.Count; i++)
        {
            var routes = _routeInfoList[i].Items.Select(ri => ri.SpecifiedRoute);
            routeInfoList.Add(GetRouteString(routes));
        }

        return routeInfoList;
    }

    /// <inheritdoc cref="INavigator.GetRouteOptions"/>
    public RouteOptions GetRouteOptions()
    {
        EnsureThreadAccess();
        return CurrentRouteInfo?.Options ?? RouteOptions.Empty;
    }

    /// <inheritdoc cref="INavigator.TryGetRouteParameter{TViewModel, TParam}(RouteBase{TViewModel, TParam}, out TParam)"/>
    public bool TryGetRouteParameter<TViewModel, TParam>(RouteBase<TViewModel, TParam> route, [MaybeNullWhen(false)] out TParam parameter)
        where TViewModel : class, IRoutedViewModel<TParam>
        where TParam : notnull
    {
        EnsureThreadAccess();
        var routeItems = CurrentRouteInfo?.Items ?? [];

        for (int i = routeItems.Length - 1; i >= 0; i--)
        {
            var specifiedRoute = routeItems[i].SpecifiedRoute;

            if (specifiedRoute.Route == route && specifiedRoute is IParameterizedConcreteRoute<TViewModel, TParam> paramSpecifiedRoute)
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
}
