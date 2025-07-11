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

    private readonly FrozenDictionary<Type, ViewInfo> _viewModelTypeToViewInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _viewModelTypeToDialogActivator;
    private readonly ImmutableArray<RoutePart> _routeParts;

    private readonly int _maxStackSize;
    private readonly int _maxBackStackCachedViewDepth;
    private readonly int _maxForwardStackCachedViewDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogTcsStack = [];

    private readonly List<ConcreteRoute> _routeStack = [];
    private int _currentRouteIndex = -1;

    private CancellationTokenSource? _navigationCts;

    private bool _blockNavigation;
    private bool _blockDialogs;

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
        TaskRunner = new TaskRunner(busy => _viewNavigator.NavigationControl.IsEnabled = !busy);

        var builder = new NavigatorBuilder();
        buildAction(builder);
        builder.Validate();

        _routeParts = [.. builder.RouteParts];
        _viewModelTypeToViewInfo = builder.ViewModelTypeToViewInfo.ToFrozenDictionary();

        IEnumerable<KeyValuePair<Type, Func<ContentDialog>>> vmTypeToDialogActivator = builder.ViewModelTypeToDialogActivator;

        if (!builder.ViewModelTypeToDialogActivator.ContainsKey(typeof(MessageDialogViewModel)))
            vmTypeToDialogActivator = vmTypeToDialogActivator.Append(new(typeof(MessageDialogViewModel), () => new MessageDialog()));

        _viewModelTypeToDialogActivator = vmTypeToDialogActivator.ToFrozenDictionary();
        _maxStackSize = builder.MaxNavigationStacksSize;
        _maxBackStackCachedViewDepth = builder.MaxBackStackCachedViewDepth;
        _maxForwardStackCachedViewDepth = builder.MaxForwardStackCachedViewDepth;
    }

    /// <inheritdoc cref="INavigator.CanGoBack"/>
    public bool CanGoBack
    {
        get {
            EnsureThreadAccess();

            if (IsNavigating)
                return false;

            if (IsShowingDialog)
                return _dialogTcsStack.Peek().Dialog.DataContext is IDismissableDialogViewModel;

            return HasBackHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanGoForward"/>
    public bool CanGoForward
    {
        get {
            EnsureThreadAccess();

            if (IsNavigating || IsShowingDialog)
                return false;

            return HasForwardHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanRefresh"/>
    public bool CanRefresh
    {
        get {
            EnsureThreadAccess();
            return !IsNavigating && !IsShowingDialog && CurrentRouteInternal is not null;
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
            return _currentRouteIndex < _routeStack.Count - 1;
        }
    }

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
            return _dialogTcsStack.Count > 0;
        }
    }

    /// <inheritdoc cref="INavigator.TaskRunner"/>
    public ITaskRunner TaskRunner { get; }

    /// <inheritdoc/>
    public IConcreteRoute? CurrentRoute
    {
        get {
            EnsureThreadAccess();
            return CurrentRouteInternal;
        }
    }

    private ConcreteRoute? CurrentRouteInternal => _currentRouteIndex >= 0 ? _routeStack[_currentRouteIndex] : null;

    /// <inheritdoc cref="INavigator.ClearHistory"/>
    public void ClearHistory()
    {
        EnsureThreadAccess();

        if (_routeStack.Count <= 0)
            return;

        var current = _routeStack[_currentRouteIndex];

        using (new PropertyChangedNotifier(this, OnPropertyChanged))
        {
            _routeStack.Clear();
            _routeStack.Add(current);
            _currentRouteIndex = 0;
        }
    }

    /// <inheritdoc cref="INavigator.GetBackStack"/>
    public IReadOnlyList<IConcreteRoute> GetBackStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex <= 0)
            return [];

        var stack = _routeStack[.._currentRouteIndex];
        stack.Reverse();
        return stack;
    }

    /// <inheritdoc cref="INavigator.GetForwardStack"/>
    public IReadOnlyList<IConcreteRoute> GetForwardStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex >= _routeStack.Count - 1)
            return [];

        return _routeStack[(_currentRouteIndex + 1)..];
    }

    /// <inheritdoc cref="INavigator.TryGetCurrentRouteParameter{TViewModel, TParam}(RoutePart{TViewModel, TParam}, out TParam)"/>
    public bool TryGetCurrentRouteParameter<TViewModel, TParam>(RoutePart<TViewModel, TParam> routePart, [MaybeNullWhen(false)] out TParam parameter)
        where TViewModel : class, IRoutedViewModel<TParam>
        where TParam : notnull
    {
        EnsureThreadAccess();
        var routeItems = CurrentRouteInternal?.Items ?? [];

        for (int i = routeItems.Length - 1; i >= 0; i--)
        {
            var concreteRoute = routeItems[i].ConcreteRoutePart;

            if (concreteRoute.RoutePart == routePart && concreteRoute is IParameterizedConcreteRoute<TViewModel, TParam> paramConcreteRoute)
            {
                parameter = paramConcreteRoute.Parameter;
                return true;
            }
        }

        parameter = default;
        return false;
    }

    /// <inheritdoc cref="INavigator.TryGetCurrentRouteViewModel{TViewModel}(out TViewModel)"/>
    public bool TryGetCurrentRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel)
        where TViewModel : class
    {
        EnsureThreadAccess();

        var routeItem = CurrentRouteInternal?.Items
            .LastOrDefault(i => i.ConcreteRoutePart.RoutePart.ViewModelType.IsAssignableTo(typeof(TViewModel)));

        if (routeItem is not null)
        {
            routeItem.EnsureViewCreatedAndModelInitialized(this);
            viewModel = (TViewModel)routeItem.ViewModel;
            return true;
        }

        viewModel = null;
        return false;
    }

    internal static string GetPath(IEnumerable<IConcreteRoutePart> routeParts) => string.Join("/", routeParts.Select(r => r.ToString()).Where(r => r.Length > 0));

    private void EnsureThreadAccess()
    {
        if (_viewNavigator.NavigationControl.DispatcherQueue?.HasThreadAccess is not true)
            throw new InvalidOperationException("Navigator can only be accessed from the UI thread.");
    }

    private bool CloseLightDismissPopups()
    {
        var xamlRoot = _viewNavigator.NavigationControl.XamlRoot;
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
