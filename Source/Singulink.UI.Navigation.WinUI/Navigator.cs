using System.Collections.Frozen;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <inheritdoc cref="INavigator"/>
public partial class Navigator : INavigator
{
    private static readonly PropertyChangedEventArgs IsNavigatingChangedArgs = new(nameof(IsNavigating));
    private static readonly PropertyChangedEventArgs IsShowingDialogChangedArgs = new(nameof(IsShowingDialog));
    private static readonly PropertyChangedEventArgs HasBackHistoryChangedArgs = new(nameof(HasBackHistory));
    private static readonly PropertyChangedEventArgs HasForwardHistoryChangedArgs = new(nameof(HasForwardHistory));
    private static readonly PropertyChangedEventArgs CanUserGoBackChangedArgs = new(nameof(CanUserGoBack));
    private static readonly PropertyChangedEventArgs CanUserGoForwardChangedArgs = new(nameof(CanUserGoForward));
    private static readonly PropertyChangedEventArgs CanUserRefreshChangedArgs = new(nameof(CanUserRefresh));

    private readonly IViewNavigator _rootViewNavigator;

    private readonly FrozenDictionary<Type, ViewInfo> _vmTypeToViewInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _vmTypeToDialogCtorFunc;
    private readonly ImmutableArray<RouteBase> _routes;
    private readonly int _maxBackStackDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogInfoStack = [];
    private readonly List<RouteInfo> _routeInfoList = [];
    private int _currentRouteIndex = -1;

    private bool _blockNavigation;
    private CancellationTokenSource? _navigationCts;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private RouteInfo? CurrentRouteInfo => _currentRouteIndex >= 0 && _currentRouteIndex < _routeInfoList.Count ? _routeInfoList[_currentRouteIndex] : null;

    /// <inheritdoc cref="INavigator.IsNavigating"/>
    public bool IsNavigating => _blockNavigation || _navigationCts is not null;

    /// <inheritdoc cref="INavigator.IsShowingDialog"/>
    public bool IsShowingDialog => _dialogInfoStack.Count > 0;

    /// <inheritdoc cref="INavigator.HasBackHistory"/>
    public bool HasBackHistory => _currentRouteIndex > 0;

    /// <inheritdoc cref="INavigator.HasForwardHistory"/>
    public bool HasForwardHistory => _currentRouteIndex < _routeInfoList.Count - 1;

    /// <inheritdoc cref="INavigator.CanUserGoBack"/>
    public bool CanUserGoBack
    {
        get
        {
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
        get
        {
            if (IsNavigating || IsShowingDialog)
                return false;

            return HasForwardHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanUserRefresh"/>
    public bool CanUserRefresh => !IsNavigating && !IsShowingDialog && CurrentRouteInfo is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root frame and mappings provided in the build action.
    /// </summary>
    public Navigator(Frame rootFrame, Action<NavigatorBuilder> buildAction) : this(new FrameNavigator(rootFrame), buildAction) { }

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
        where TViewModel : class, IRoutedViewModel<TParam>
    {
        var lastRoute = CurrentRouteInfo?.Items
            .Select(ri => ri.SpecifiedRoute)
            .OfType<IParameterizedSpecifiedRoute<TParam, TViewModel>>()
            .LastOrDefault(r => r.Route == route);

        return lastRoute is not null ? lastRoute.Parameter : throw new InvalidOperationException($"Current route path does not contain the specified route.");
    }

    /// <inheritdoc cref="INavigator.GetRouteViewModel{TViewModel}"/>
    public TViewModel GetRouteViewModel<TViewModel>()
        where TViewModel : class
    {
        var viewModel = CurrentRouteInfo?.Items.Select(ri => ri.ViewModel).OfType<TViewModel>().LastOrDefault();
        return viewModel ?? throw new InvalidOperationException($"Current route does not contain a view model of type '{typeof(TViewModel)}'.");
    }

    /// <inheritdoc cref="INavigator.GoBackAsync"/>
    public async Task<NavigationResult> GoBackAsync(bool userInitiated)
    {
        if (userInitiated)
        {
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
        if (userInitiated && !CanUserGoForward)
            return NavigationResult.Cancelled;

        return await NavigateAsync(NavigationType.Forward, null, null);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync(string)"/>
    public async Task<NavigationResult> NavigateAsync(string route)
    {
        string anchor = null;
        int anchorIndex = route.IndexOf('#');

        if (anchorIndex >= 0)
        {
            anchor = Uri.UnescapeDataString(route[(anchorIndex + 1)..]);
            route = route[..anchorIndex];
        }

        // TODO: Implement support for query parameters

        int queryIndex = route.IndexOf('?');

        if (queryIndex >= 0)
            route = route[..queryIndex];

        if (!TryMatchRoute(route, out var requestedSpecifiedRouteItems))
            throw new ArgumentException($"No route found for '{route}'.", nameof(route));

        var routeOptions = anchor is not null ? new RouteOptions(anchor) : null;

        return await NavigateAsync(NavigationType.New, requestedSpecifiedRouteItems, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TViewModel}(ISpecifiedRootRoute{TViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TViewModel>(ISpecifiedRootRoute<TViewModel> route, RouteOptions? routeOptions = null)
        where TViewModel : class
    {
        return await NavigateNewWithEnsureMatched([route], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class
    {
        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
    {
        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class
    {
        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync(RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync(RouteOptions routeOptions)
    {
        return await NavigateAsync(NavigationType.New, null, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    /// <inheritdoc cref="INavigator.RefreshAsync"/>
    public async Task<NavigationResult> RefreshAsync(bool userInitiated)
    {
        if (userInitiated && IsNavigating)
            return NavigationResult.Cancelled;

        return await NavigateAsync(NavigationType.Refresh, null, null);
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel}, out TViewModel)"/>"
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
        where TViewModel : class
    {
        return ShowDialogAsync(null, getModelFunc, out viewModel);
    }

    internal Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
    {
        EnsureThreadAccess();

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

    private string GetRouteString(List<ISpecifiedRoute> routes) => string.Join("/", routes.Select(r => r.ToString()).Where(r => !string.IsNullOrEmpty(r)));

    private async Task<NavigationResult> NavigateNewWithEnsureMatched(List<ISpecifiedRoute> specifiedRouteItems, RouteOptions? routeOptions)
    {
        string routeString = GetRouteString(specifiedRouteItems);

        if (!TryMatchRoute(routeString, out var requestedRoutes))
            throw new ArgumentException($"No route found for '{routeString}'.", nameof(specifiedRouteItems));

        if (!requestedRoutes.SequenceEqual(specifiedRouteItems))
        {
            string message = $"Route '{routeString}' matched a different route than what was specified. Routes may be misconfigured, missing or specified in an incorrect order.";
            throw new ArgumentException(message, nameof(specifiedRouteItems));
        }

        return await NavigateAsync(NavigationType.New, requestedRoutes, routeOptions);
    }

    private async Task<NavigationResult> NavigateAsync(NavigationType navigationType, List<ISpecifiedRoute>? requestedSpecifiedRouteItems, RouteOptions? routeOptions)
    {
        EnsureThreadAccess();

        if (_dialogInfoStack.Count > 0)
            throw new InvalidOperationException("Cannot navigate while a dialog is shown.");

        if (_blockNavigation)
            throw new InvalidOperationException($"Navigation requested at invalid time. Navigation cannot be requested during view construction or view model '{nameof(IRoutedViewModelBase.OnNavigatedFrom)}' execution.");

        var lastRouteInfo = CurrentRouteInfo;

        RouteInfo routeInfo;

        if (navigationType is NavigationType.New)
        {
            routeOptions ??= RouteOptions.Empty;

            if (requestedSpecifiedRouteItems is not null)
                routeInfo = BuildRouteInfo(requestedSpecifiedRouteItems, routeOptions);
            else if (CurrentRouteInfo is not null)
                routeInfo = new RouteInfo(CurrentRouteInfo.Items, routeOptions);
            else
                throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");
        }
        else
        {
            Debug.Assert(requestedSpecifiedRouteItems is null, "Route items should only be provided for new navigations.");
            Debug.Assert(routeOptions is null, "Route options should only be provided for new navigations.");

            if (navigationType is NavigationType.Back)
            {
                if (!HasBackHistory)
                    throw new InvalidOperationException("Cannot navigate back because there is no previous view.");

                routeInfo = _routeInfoList[_currentRouteIndex - 1];
            }
            else if (navigationType is NavigationType.Forward)
            {
                if (!HasForwardHistory)
                    throw new InvalidOperationException("Cannot navigate forward because there is no next view.");

                routeInfo = _routeInfoList[_currentRouteIndex + 1];
            }
            else if (navigationType is NavigationType.Refresh)
            {
                routeInfo = lastRouteInfo ?? throw new InvalidOperationException("Cannot refresh when no route is currently active.");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(navigationType));
            }
        }

        if (navigationType is not NavigationType.Refresh && lastRouteInfo is not null)
        {
            // Find the index where the route diverges from the last route

            int maxCommonItems = Math.Min(lastRouteInfo.Items.Length, routeInfo.Items.Length);
            int i;

            for (i = 0; i < maxCommonItems; i++)
            {
                var lastRouteInfoItem = lastRouteInfo.Items[i];
                var routeInfoItem = routeInfo.Items[i];

                if (!lastRouteInfoItem.SpecifiedRoute.Equals(routeInfoItem.SpecifiedRoute))
                    break;
            }

            // Notify view models of navigating away from the last route, starting from the end of the route

            _blockNavigation = true;

            for (int j = lastRouteInfo.Items.Length - 1; j >= i; j--)
            {
                if (lastRouteInfo.Items[j].View is IRoutedView view && view.Model is IRoutedViewModelBase viewModel)
                {
                    var args = new NavigatingCancelArgs();
                    await viewModel.OnNavigatingFromAsync(args);

                    if (_dialogInfoStack.Count > 0)
                        throw new InvalidOperationException("All dialogs must be closed before completing navigating away from a view model.");

                    if (args.Cancel)
                        return NavigationResult.Cancelled;
                }
            }

            _blockNavigation = false;
        }

        bool cancelledLastNavigation = false;

        if (_navigationCts is not null)
        {
            _navigationCts.Cancel();
            cancelledLastNavigation = true;
        }

        var cancelNavigateToken = (_navigationCts = new()).Token;

        bool canGoForward = HasForwardHistory;
        bool canGoBack = HasBackHistory;

        if (navigationType is NavigationType.New)
        {
            if (cancelledLastNavigation)
            {
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
            _currentRouteIndex--;
        }
        else if (navigationType is NavigationType.Forward)
        {
            _currentRouteIndex++;
        }

        if (canGoBack != HasBackHistory)
            OnPropertyChanged(HasBackHistoryChangedArgs);

        if (canGoForward != HasForwardHistory)
            OnPropertyChanged(HasForwardHistoryChangedArgs);

        List<RouteInfoItem> navigateFromItems = null;

        if (lastRouteInfo is not null)
        {
            for (int i = 0; i < lastRouteInfo.Items.Length; i++)
            {
                if (i >= routeInfo.Items.Length || lastRouteInfo.Items[i] != routeInfo.Items[i])
                {
                    navigateFromItems = lastRouteInfo.Items.Skip(i).ToList();
                    break;
                }
            }
        }

        var viewNavigator = _rootViewNavigator;

        for (int i = 0; i < routeInfo.Items.Length; i++)
        {
            if (viewNavigator is null)
                throw new UnreachableException("Unexpected null nested view navigator.");

            var routeItemInfo = routeInfo.Items[i];
            bool hasNestedNavigation = i < routeInfo.Items.Length - 1;

            _blockNavigation = true;

            routeItemInfo.EnsureViewCreatedAndModelInitialized();

            if (!routeItemInfo.AlreadyNavigatedTo)
                viewNavigator.SetActiveView(routeItemInfo.View);

            _blockNavigation = false;

            var args = new NavigationArgs(routeItemInfo.IsInitialNavigation, routeItemInfo.AlreadyNavigatedTo, hasNestedNavigation, routeInfo.Options, navigationType);
            await routeItemInfo.ViewModel.OnNavigatedToAsync(args);

            routeItemInfo.IsInitialNavigation = false;

            if (cancelNavigateToken.IsCancellationRequested)
                return NavigationResult.Rerouted;

            routeItemInfo.AlreadyNavigatedTo = true;

            if (hasNestedNavigation && _dialogInfoStack.Count > 0)
                throw new InvalidOperationException("All dialogs must be closed before completing navigation to a view with nested navigations.");

            viewNavigator = routeItemInfo.NestedViewNavigator;
        }

        _navigationCts = null;
        TrimRouteInfoList();

        return NavigationResult.Success;

        RouteInfo BuildRouteInfo(List<ISpecifiedRoute> requestedSpecifiedRouteItems, RouteOptions routeOptions)
        {
            var commonItemInfoCandidates = _routeInfoList;
            int i; // number of common items

            for (i = 0; i < requestedSpecifiedRouteItems.Count; i++)
            {
                var newCandidates = commonItemInfoCandidates
                    .Where(ri => i < ri.Items.Length && ri.Items[i].SpecifiedRoute.Equals(requestedSpecifiedRouteItems[i])).ToList();

                if (newCandidates.Count is 0)
                    break;

                commonItemInfoCandidates = newCandidates;
            }

            var routeInfoItems = new RouteInfoItem?[requestedSpecifiedRouteItems.Count];

            // Copy common items

            if (i > 0)
                commonItemInfoCandidates[0].Items.CopyTo(routeInfoItems!, i);

            // Create remaining items

            for (; i < routeInfoItems.Length; i++)
            {
                var item = requestedSpecifiedRouteItems[i];
                var createViewFunc = _vmTypeToViewInfo[item.Route.ViewModelType].CreateView;
                routeInfoItems[i] = new(item, createViewFunc);
            }

            return new RouteInfo(Unsafe.As<RouteInfoItem?[], ImmutableArray<RouteInfoItem>>(ref routeInfoItems), routeOptions);
        }

        void TrimRouteInfoList()
        {
            // TODO: Optimization - Remove trimmed views that can no longer be navigated to from view navigators that might cache them.

            if (_routeInfoList.Count > _maxBackStackDepth)
            {
                int trimCount = _routeInfoList.Count - _maxBackStackDepth;
                _routeInfoList.RemoveRange(0, trimCount);
                _currentRouteIndex -= trimCount;
            }
        }
    }

    private async Task<NavigationResult> NavigatePartialAsync(Type parentViewModelType, List<ISpecifiedRoute> requestedNestedRoutes, RouteOptions? routeOptions)
    {
        var currentRouteInfo = CurrentRouteInfo ?? throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");

        int parentRouteItemIndex = currentRouteInfo.Items.FindLastIndex(ri => ri.SpecifiedRoute.Route.ViewModelType == parentViewModelType);

        if (parentRouteItemIndex < 0)
            throw new InvalidOperationException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        var routes = currentRouteInfo.Items.Take(parentRouteItemIndex + 1).Select(ri => ri.SpecifiedRoute).Concat(requestedNestedRoutes).ToList();
        return await NavigateNewWithEnsureMatched(routes, routeOptions);
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

    private class RouteInfo(ImmutableArray<RouteInfoItem> items, RouteOptions options)
    {
        public ImmutableArray<RouteInfoItem> Items { get; } = items;

        public RouteOptions Options { get; } = options;
    }

    private class RouteInfoItem(ISpecifiedRoute route, Func<UIElement> createViewFunc)
    {
        private readonly Func<UIElement> _createViewFunc = createViewFunc;

        public ISpecifiedRoute SpecifiedRoute { get; } = route;

        public UIElement? View { get; private set; }

        public IRoutedViewModelBase? ViewModel => (View as IRoutedView)?.Model;

        public bool IsInitialNavigation { get; set; } = true;

        public bool AlreadyNavigatedTo { get; set; }

        public IViewNavigator? NestedViewNavigator => (View as IParentView)?.CreateNestedViewNavigator();

        [MemberNotNull(nameof(View))]
        [MemberNotNull(nameof(ViewModel))]
        public void EnsureViewCreatedAndModelInitialized()
        {
            View ??= _createViewFunc();
            Debug.Assert(ViewModel is not null, "View model should not be null.");
            SpecifiedRoute.Route.InitializeViewModel(ViewModel, SpecifiedRoute);
        }
    }

    private struct PropertyChangedNotifier
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
                _navigator = null;
                Update();
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
