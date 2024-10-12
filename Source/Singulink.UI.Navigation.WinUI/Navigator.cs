using System.Collections.Frozen;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <inheritdoc cref="INavigator"/>
public class Navigator : INavigator
{
    private static readonly PropertyChangedEventArgs IsShowingDialogChangedArgs = new(nameof(IsShowingDialog));
    private static readonly PropertyChangedEventArgs CanGoBackChangedArgs = new(nameof(CanGoBack));
    private static readonly PropertyChangedEventArgs CanGoForwardChangedArgs = new(nameof(CanGoForward));

    private readonly IViewNavigator _rootViewNavigator;

    private readonly FrozenDictionary<Type, ViewInfo> _vmTypeToViewInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _vmTypeToDialogCtorFunc;
    private readonly ImmutableArray<RouteBase> _routes;
    private readonly int _maxBackStackDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogInfoStack = [];
    private readonly List<RouteInfo> _routeInfoList = [];
    private int _currentRouteIndex = -1;
    private int _currentRouteItemIndex = -1;

    private CancellationTokenSource? _navigatingCts;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private RouteInfo? CurrentRouteInfo => _currentRouteIndex >= 0 && _currentRouteIndex < _routeInfoList.Count ? _routeInfoList[_currentRouteIndex] : null;

    /// <inheritdoc cref="INavigator.IsShowingDialog"/>
    public bool IsShowingDialog => _dialogInfoStack.Count > 0;

    /// <inheritdoc cref="INavigator.CanGoBack"/>
    public bool CanGoBack => _currentRouteIndex > 0;

    /// <inheritdoc cref="INavigator.CanGoForward"/>
    public bool CanGoForward => _currentRouteIndex >= 0 && _currentRouteIndex < _routeInfoList.Count - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root frame and mappings provided in the build action.
    /// </summary>
    public Navigator(Frame rootFrame, Action<NavigatorBuilder> buildAction) : this(new FrameNavigator(() => rootFrame), buildAction) { }

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
    public RouteOptions? GetRouteOptions() => CurrentRouteInfo?.Options;

    /// <inheritdoc cref="INavigator.GetRouteParameter{TParam, TViewModel}(RouteBase{TParam, TViewModel})"/>
    public TParam GetRouteParameter<TParam, TViewModel>(RouteBase<TParam, TViewModel> route)
        where TParam : notnull
        where TViewModel : IRoutedViewModel<TParam>
    {
        var lastRoute = CurrentRouteInfo?.Items.Select(ri => ri.SpecifiedRoute).LastOrDefault(r => r == route) as IParameterizedSpecifiedRoute<TParam, TViewModel>;
        return lastRoute is not null ? lastRoute.Parameter : throw new InvalidOperationException($"Current route path does not contain the specified route.");
    }

    /// <inheritdoc cref="INavigator.GetRouteViewModel{TViewModel}"/>
    public TViewModel GetRouteViewModel<TViewModel>()
        where TViewModel : IRoutedViewModel
    {
        var lastView = CurrentRouteInfo?.Items.Select(ri => ri.View).OfType<IRoutedView<TViewModel>>().LastOrDefault();
        return lastView is not null ? lastView.Model : throw new InvalidOperationException($"Current route does not contain a view model of type '{typeof(TViewModel)}'.");
    }

    /// <inheritdoc cref="INavigator.GoBackAsync"/>
    public async Task GoBackAsync()
    {
        if (!CanGoBack)
            throw new InvalidOperationException("Cannot navigate back because there is no previous view.");

        var backRouteInfo = _routeInfoList[_currentRouteIndex - 1];
        await NavigateAsync(NavigationType.Back, backRouteInfo.Items.Select(ri => ri.SpecifiedRoute).ToList(), backRouteInfo.Options);
    }

    /// <inheritdoc cref="INavigator.GoForwardAsync"/>
    public async Task GoForwardAsync()
    {
        if (!CanGoForward)
            throw new InvalidOperationException("Cannot navigate forward because there is no next view.");

        var forwardRouteInfo = _routeInfoList[_currentRouteIndex + 1];
        await NavigateAsync(NavigationType.Forward, forwardRouteInfo.Items.Select(ri => ri.SpecifiedRoute).ToList(), forwardRouteInfo.Options);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync(string)"/>
    public async Task NavigateAsync(string route)
    {
        int anchorIndex = route.IndexOf('#');
        string anchor = null;

        if (anchorIndex >= 0)
        {
            anchor = route[(anchorIndex + 1)..];
            route = route[..anchorIndex];
        }

        // TODO: Implement support for query parameters

        int queryIndex = route.IndexOf('?');

        if (queryIndex >= 0)
            route = route[..queryIndex];

        if (!TryMatchRoute(route, out var requestedSpecifiedRouteItems))
            throw new ArgumentException($"No route found for '{route}'.", nameof(route));

        var routeOptions = anchor is not null ? new RouteOptions(anchor) : null;

        await NavigateAsync(NavigationType.New, requestedSpecifiedRouteItems, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TViewModel}(ISpecifiedRoute{TViewModel}, RouteOptions)"/>
    public async Task NavigateAsync<TViewModel>(ISpecifiedRoute<TViewModel> route, RouteOptions? routeOptions = null)
        where TViewModel : IRoutedViewModel
    {
        await NavigateNewWithEnsureMatched([route], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel}(ISpecifiedRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public async Task NavigateAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel : IRoutedViewModel
    {
        await NavigateNewWithEnsureMatched([parentRoute, nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
    {
        await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
        where TNestedViewModel3 : IRoutedViewModel
    {
        await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public Task NavigatePartialAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel : IRoutedViewModel
    {
        return NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
    {
        await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
        where TNestedViewModel3 : IRoutedViewModel
    {
        await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel}, out TViewModel)"/>"
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
    {
        return ShowDialogAsync(null, getModelFunc, out viewModel);
    }

    internal Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
    {
        EnsureThreadAccess();

        if (_currentRouteItemIndex != CurrentRouteInfo?.Items.Length - 1)
            throw new InvalidOperationException("Cannot show a dialog before the current route has been fully navigated.");

        bool hasParentDialog = _dialogInfoStack.TryPeek(out var parentDialogInfo);

        if (requestingParentDialog != parentDialogInfo.Dialog)
        {
            if (requestingParentDialog is null)
                throw new InvalidOperationException("Another dialog is currently being shown. Nested dialogs must be shown using the dialog navigator of the parent dialog.");
            else
                throw new InvalidOperationException("Dialog cannot show a nested dialog because it is not the currently top showing dialog.");
        }

        if (!_vmTypeToDialogCtorFunc.TryGetValue(typeof(TViewModel), out var ctorFunc))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{typeof(TViewModel)}'.");

        var dialog = ctorFunc.Invoke();
        var dialogNavigator = new DialogNavigator(this, dialog);

        dialog.XamlRoot = _rootViewNavigator.XamlRoot;
        dialog.DataContext = viewModel = getModelFunc(dialogNavigator);

        var dismissableViewModel = viewModel as IDismissableDialogViewModel;

        dialog.PrimaryButtonClick += OnButtonClick;
        dialog.SecondaryButtonClick += OnButtonClick;
        dialog.CloseButtonClick += OnButtonClick;
        dialog.Closing += OnClosing;

        return ShowAsync(dialog);

        async Task ShowAsync(ContentDialog dialog)
        {
            var tcs = new TaskCompletionSource();
            _dialogInfoStack.Push((dialog, tcs));

            if (hasParentDialog)
                parentDialogInfo.Dialog.Hide();

            _ = dialog.ShowAsync();

            if (_dialogInfoStack.Count is 1)
                OnPropertyChanged(IsShowingDialogChangedArgs);

            await tcs.Task;
        }

        void OnButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_dialogInfoStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
                args.Cancel = true;
        }

        void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_dialogInfoStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;
                dismissableViewModel?.OnDismissRequested();
            }
        }
    }

    internal void CloseDialog(ContentDialog dialog)
    {
        EnsureThreadAccess();

        if (!_dialogInfoStack.TryPeek(out var dialogInfo) || dialogInfo.Dialog != dialog)
            throw new InvalidOperationException("Dialog is not currently the top showing dialog.");

        _dialogInfoStack.Pop();
        dialog.Hide();

        if (_dialogInfoStack.Count is 0)
            OnPropertyChanged(IsShowingDialogChangedArgs);

        dialogInfo.Tcs.SetResult();

        if (_dialogInfoStack.TryPeek(out var parentDialogInfo))
            _ = parentDialogInfo.Dialog.ShowAsync();
    }

    private void EnsureThreadAccess()
    {
        if (!_rootViewNavigator.DispatcherQueue.HasThreadAccess)
            throw new InvalidOperationException("Navigator members can only be accessed from the UI thread of the root view that this navigator is assigned to.");
    }

    private string GetRouteString(List<ISpecifiedRoute> routes) => string.Join("/", routes);

    private async Task NavigateNewWithEnsureMatched(List<ISpecifiedRoute> specifiedRouteItems, RouteOptions? routeOptions)
    {
        string routeString = GetRouteString(specifiedRouteItems);

        if (!TryMatchRoute(routeString, out var requestedRoutes))
            throw new ArgumentException($"No route found for '{routeString}'.", nameof(specifiedRouteItems));

        if (!requestedRoutes.SequenceEqual(specifiedRouteItems))
            throw new ArgumentException($"Route '{routeString}' matched a different route than expected.", nameof(specifiedRouteItems));

        await NavigateAsync(NavigationType.New, requestedRoutes, routeOptions);
    }

    private async Task NavigateAsync(NavigationType navigationType, List<ISpecifiedRoute>? requestedSpecifiedRouteItems, RouteOptions? routeOptions)
    {
        EnsureThreadAccess();

        if (_dialogInfoStack.Count > 0)
            throw new InvalidOperationException("Cannot navigate while a dialog is being shown.");

        bool cancelledLastNavigation = false;

        if (_navigatingCts is not null)
        {
            _navigatingCts.Cancel();
            cancelledLastNavigation = true;
        }

        var cancelNavigateToken = (_navigatingCts = new()).Token;

        bool canGoForward = CanGoForward;
        bool canGoBack = CanGoBack;

        RouteInfo routeInfo;

        if (navigationType is NavigationType.New)
        {
            Debug.Assert(requestedSpecifiedRouteItems is not null, "Expected route items for new navigation.");
            routeInfo = BuildRouteInfo(requestedSpecifiedRouteItems, routeOptions);

            if (cancelledLastNavigation)
            {
                if (_currentRouteIndex < 0 || _currentRouteIndex > _routeInfoList.Count)
                    throw new UnreachableException("Invalid current route index upon navigation cancellation.");

                _routeInfoList[_currentRouteIndex] = routeInfo;
            }
            else
            {
                if (canGoForward)
                    _routeInfoList.RemoveRange(_currentRouteIndex + 1, _routeInfoList.Count - _currentRouteIndex - 1);

                _routeInfoList.Add(routeInfo);
                _currentRouteIndex = _routeInfoList.Count - 1;
            }
        }
        else if (navigationType is NavigationType.Back)
        {
            Debug.Assert(requestedSpecifiedRouteItems is null, "Expected null route items for back navigation.");
            Debug.Assert(routeOptions is null, "Expected null route options for back navigation.");

            routeInfo = _routeInfoList[--_currentRouteIndex];
        }
        else if (navigationType is NavigationType.Forward)
        {
            Debug.Assert(requestedSpecifiedRouteItems is null, "Expected null route items for forward navigation.");
            Debug.Assert(routeOptions is null, "Expected null route options for back navigation.");

            routeInfo = _routeInfoList[++_currentRouteIndex];
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(navigationType));
        }

        if (canGoBack != CanGoBack)
            OnPropertyChanged(CanGoBackChangedArgs);

        if (canGoForward != CanGoForward)
            OnPropertyChanged(CanGoForwardChangedArgs);

        var viewNavigator = _rootViewNavigator;

        for (int i = 0; i < routeInfo.Items.Length; i++)
        {
            if (viewNavigator is null)
                throw new UnreachableException("Unexpected null nested view navigator.");

            _currentRouteItemIndex = i;

            var routeItemInfo = routeInfo.Items[i];
            bool isInitialNavigation = routeItemInfo.CreateViewIfNeeded();

            if (cancelNavigateToken.IsCancellationRequested)
                return;

            viewNavigator.SetActiveView(routeItemInfo.View);

            if (cancelNavigateToken.IsCancellationRequested)
                return;

            var navigationArgs = new NavigationArgs(isInitialNavigation, i < routeInfo.Items.Length - 1, routeInfo.Options, navigationType);
            await routeItemInfo.SpecifiedRoute.Route.InvokeViewModelOnNavigatedToAsync(this, routeItemInfo.View, routeItemInfo.SpecifiedRoute, navigationArgs);

            if (cancelNavigateToken.IsCancellationRequested)
                return;

            viewNavigator = routeItemInfo.NestedViewNavigator;
        }

        _navigatingCts = null;
        TrimRouteInfoList();

        RouteInfo BuildRouteInfo(List<ISpecifiedRoute> requestedSpecifiedRouteItems, RouteOptions? routeOptions)
        {
            var lastRouteInfo = CurrentRouteInfo;
            var routeInfoItems = new RouteInfoItem?[requestedSpecifiedRouteItems.Count];

            lastRouteInfo?.Items.CopyTo(routeInfoItems!, Math.Min(lastRouteInfo.Items.Length, routeInfoItems.Length));

            for (int i = 0; i < routeInfoItems.Length; i++)
            {
                var requestedSpecifiedRouteItem = requestedSpecifiedRouteItems[i];
                var routeItemInfo = routeInfoItems[i];

                if (routeItemInfo is not null)
                {
                    // If the specified route item matches the copied info from the last route then reuse it, otherwise clear the rest of the copied
                    // items since this is where the route diverges.

                    if (routeItemInfo.SpecifiedRoute.Equals(requestedSpecifiedRouteItem))
                        continue;
                    else
                        Array.Clear(routeInfoItems, i, routeInfoItems.Length - i);
                }

                routeItemInfo = new(requestedSpecifiedRouteItem, _vmTypeToViewInfo[requestedSpecifiedRouteItem.Route.ViewModelType].CreateView);
            }

            return new RouteInfo(Unsafe.As<RouteInfoItem?[], ImmutableArray<RouteInfoItem>>(ref routeInfoItems), routeOptions);
        }

        void TrimRouteInfoList()
        {
            if (_routeInfoList.Count > _maxBackStackDepth)
            {
                int trimCount = _routeInfoList.Count - _maxBackStackDepth;
                _routeInfoList.RemoveRange(0, trimCount);
                _currentRouteIndex -= trimCount;
            }
        }
    }

    private async Task NavigatePartialAsync(Type parentViewModelType, List<ISpecifiedRoute> requestedNestedRoutes, RouteOptions? routeOptions)
    {
        var currentRouteInfo = CurrentRouteInfo ?? throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");

        int parentRouteItemIndex = currentRouteInfo.Items.FindLastIndex(ri => ri.SpecifiedRoute.Route.ViewModelType == parentViewModelType);

        if (parentRouteItemIndex < 0)
            throw new InvalidOperationException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        var routes = currentRouteInfo.Items.Take(parentRouteItemIndex + 1).Select(ri => ri.SpecifiedRoute).Concat(requestedNestedRoutes).ToList();
        await NavigateNewWithEnsureMatched(routes, routeOptions);
    }

    private bool TryMatchRoute(string route, [MaybeNullWhen(false)] out List<ISpecifiedRoute> specifiedRouteItems)
    {
        specifiedRouteItems = [];

        if (TryMatchRoute(route, null, specifiedRouteItems, out _))
        {
            specifiedRouteItems.Reverse();
            return true;
        }

        return false;
    }

    private bool TryMatchRoute(ReadOnlySpan<char> routeString, Type? parentViewModelType, List<ISpecifiedRoute> specifiedRouteItems, out ReadOnlySpan<char> rest)
    {
        foreach (var route in _routes.Where(r => r.ParentViewModelType == parentViewModelType))
        {
            if (route.TryMatch(routeString, out var specifiedRoute, out rest))
            {
                if (rest.Length is 0 || TryMatchRoute(rest, route.ViewModelType, specifiedRouteItems, out rest))
                {
                    specifiedRouteItems.Add(specifiedRoute);
                    return true;
                }
            }
        }

        rest = routeString;
        return false;
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

    private class RouteInfo(ImmutableArray<RouteInfoItem> items, RouteOptions? options)
    {
        public ImmutableArray<RouteInfoItem> Items { get; } = items;

        public RouteOptions? Options { get; } = options;
    }

    private class RouteInfoItem(ISpecifiedRoute route, Func<UIElement> createViewFunc)
    {
        private readonly Func<UIElement> _createViewFunc = createViewFunc;

        public ISpecifiedRoute SpecifiedRoute { get; } = route;

        public UIElement? View { get; private set; }

        public IViewNavigator? NestedViewNavigator => (View as IParentView)?.GetNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        public bool CreateViewIfNeeded()
        {
            if (View is null)
            {
                View = _createViewFunc.Invoke();
                return true;
            }

            return false;
        }
    }
}
