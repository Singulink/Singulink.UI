using System.Runtime.ExceptionServices;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// An in-memory <see cref="NavigatorCore"/> for unit testing view models without a UI framework. It runs the real navigation and dialog engine against
/// placeholder views, records everything that happens in <see cref="Events"/>, and lets tests script dialog outcomes.
/// </summary>
/// <remarks>
/// Instances must be created and used inside <see cref="NavigationTestContext.Run(Func{Task})"/>, which provides the single-threaded synchronization
/// context the navigator and its task runners require.
/// </remarks>
public sealed class TestNavigator : NavigatorCore
{
    private readonly TestViewNavigator _rootViewNavigator;
    private readonly List<NavigatorEvent> _events = [];
    private readonly List<IDialogViewModel> _showingDialogs = [];
    private readonly List<TaskRunner> _dialogTaskRunners = [];
    private readonly Dictionary<Type, Func<IDialogViewModel, Task>> _dialogScripts = [];
    private Func<MessageDialogViewModel, int>? _messageDialogScript;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestNavigator"/> class using the specified build action to map view models and add routes.
    /// </summary>
    public TestNavigator(Action<TestNavigatorBuilder> buildAction)
        : this(new TestViewNavigator(), new BusyRelay(), CreateBuilder(buildAction))
    {
    }

    private TestNavigator(TestViewNavigator rootViewNavigator, BusyRelay rootBusyRelay, TestNavigatorBuilder builder)
        : base(rootViewNavigator, new TaskRunner(rootBusyRelay.Invoke), builder)
    {
        _rootViewNavigator = rootViewNavigator;
        rootBusyRelay.Handler = busy => _events.Add(new BusyChangedEvent(null, busy));
    }

    /// <summary>
    /// Gets everything that happened in this navigator, in order: navigations, redirects, view model creation and activation, lifecycle method
    /// invocations, dialogs and busy state changes. Use <see cref="ClearEvents"/> to start a fresh slice before the part of a test being asserted on.
    /// </summary>
    public IReadOnlyList<NavigatorEvent> Events => _events;

    /// <summary>
    /// Gets the result of the most recently completed navigation, or <see langword="null"/> if no navigation has completed yet.
    /// </summary>
    public NavigationResult? LastNavigationResult { get; private set; }

    /// <summary>
    /// Gets the active view models from the root of the view hierarchy down to the leaf.
    /// </summary>
    public IReadOnlyList<IRoutedViewModelBase> ActiveViewModels
    {
        get {
            var viewModels = new List<IRoutedViewModelBase>();
            var viewNavigator = _rootViewNavigator;

            while (viewNavigator?.ActiveView is { } view)
            {
                if (view.DataContext is { } viewModel)
                    viewModels.Add(viewModel);

                viewNavigator = view.ChildNavigator;
            }

            return viewModels;
        }
    }

    /// <summary>
    /// Gets the dialogs currently showing, from the bottom of the dialog stack to the top.
    /// </summary>
    public IReadOnlyList<IDialogViewModel> ShowingDialogs => _showingDialogs;

    /// <summary>
    /// Gets the top showing dialog, or <see langword="null"/> if no dialog is showing.
    /// </summary>
    public IDialogViewModel? TopDialog => _showingDialogs.Count > 0 ? _showingDialogs[^1] : null;

    /// <summary>
    /// Gets or sets a value indicating whether message dialogs without a scripted response (see <see cref="OnMessageDialog"/>) are automatically answered
    /// with their default button (or the first button if there is no default). When <see langword="false"/> (the default), an unscripted message dialog
    /// fails the test, since it usually indicates an unexpected code path or a missing script.
    /// </summary>
    public bool AutoAcceptMessageDialogs { get; set; }

    /// <summary>
    /// Gets the active view model of the specified type, searching from the leaf of the view hierarchy upwards.
    /// </summary>
    /// <exception cref="InvalidOperationException">No active view model of the specified type was found.</exception>
    public TViewModel ActiveViewModel<TViewModel>() where TViewModel : class, IRoutedViewModelBase
    {
        return ActiveViewModels.OfType<TViewModel>().LastOrDefault() ??
            throw new InvalidOperationException($"No active view model of type '{typeof(TViewModel)}'.");
    }

    /// <summary>
    /// Removes all recorded events.
    /// </summary>
    public void ClearEvents() => _events.Clear();

    /// <summary>
    /// Registers a script that runs whenever a dialog with the specified view model type is shown. The script typically sets state on the view model and
    /// closes it through its navigator. Scripts run asynchronously after the dialog has been shown, so the code that showed the dialog is already awaiting
    /// it. Dialogs without a script stay open so the test can drive them through <see cref="TopDialog"/>.
    /// </summary>
    public void OnDialogShown<TViewModel>(Action<TViewModel> script) where TViewModel : class, IDialogViewModel
    {
        OnDialogShown<TViewModel>(viewModel => {
            script(viewModel);
            return Task.CompletedTask;
        });
    }

    /// <inheritdoc cref="OnDialogShown{TViewModel}(Action{TViewModel})"/>
    public void OnDialogShown<TViewModel>(Func<TViewModel, Task> script) where TViewModel : class, IDialogViewModel
    {
        _dialogScripts[typeof(TViewModel)] = viewModel => script((TViewModel)viewModel);
    }

    /// <summary>
    /// Registers a script that chooses the button index to answer message dialogs with. Runs whenever a message dialog is shown.
    /// </summary>
    public void OnMessageDialog(Func<MessageDialogViewModel, int> script) => _messageDialogScript = script;

    /// <summary>
    /// Delivers a dismiss request to the top dialog, equivalent to the user pressing the escape key or the system back button. The request is only
    /// honoured if the dialog view model implements <see cref="IDismissibleDialogViewModel"/> and the dialog is not busy, matching the application
    /// navigator's behaviour.
    /// </summary>
    /// <exception cref="InvalidOperationException">No dialog is showing.</exception>
    public void RequestDismissTop()
    {
        var top = TopDialog ?? throw new InvalidOperationException("No dialog is showing.");
        _events.Add(new DialogDismissRequestedEvent(top));
        TryDismissTopDialog();
    }

    /// <summary>
    /// Waits until the navigator and all dialogs are idle: no busy tasks, no fire-and-forget work and no pending continuations. Use this after invoking
    /// commands that start work without returning a task before asserting on the outcome.
    /// </summary>
    public async Task WaitUntilIdleAsync()
    {
        while (true)
        {
            await TaskRunner.WaitForIdleAsync(waitForNonBusyTasks: true);

            foreach (var dialogTaskRunner in _dialogTaskRunners.ToArray())
                await dialogTaskRunner.WaitForIdleAsync(waitForNonBusyTasks: true);

            await Task.Yield();

            if (!TaskRunner.IsBusy && !_dialogTaskRunners.Any(r => r.IsBusy))
                return;
        }
    }

    /// <inheritdoc/>
    protected override void EnsureThreadAccess()
    {
        if (SynchronizationContext.Current is null)
        {
            throw new InvalidOperationException(
                "The test navigator must be used inside NavigationTestContext.Run(), which provides the single-threaded synchronization context it requires.");
        }
    }

    /// <inheritdoc/>
    protected override bool CloseLightDismissPopups() => false;

    /// <inheritdoc/>
    protected override void WireView(object view, IRoutedViewModelBase viewModel, out object? childViewNavigator)
    {
        var testView = (TestParentView)view;
        testView.DataContext = viewModel;
        childViewNavigator = testView.ChildNavigator;
        _events.Add(new ViewModelCreatedEvent(viewModel));
    }

    /// <inheritdoc/>
    protected override void SetActiveView(object viewNavigator, object? view)
    {
        var testViewNavigator = (TestViewNavigator)viewNavigator;
        var newView = (TestParentView?)view;

        if (ReferenceEquals(testViewNavigator.ActiveView, newView))
            return;

        if (testViewNavigator.ActiveView?.DataContext is { } previousViewModel)
            _events.Add(new ViewModelDeactivatedEvent(previousViewModel));

        testViewNavigator.ActiveView = newView;

        if (newView?.DataContext is { } viewModel)
            _events.Add(new ViewModelActivatedEvent(viewModel));
    }

    /// <inheritdoc/>
    protected override void WireDialog(object dialog, IDialogViewModel viewModel, out ITaskRunner taskRunner)
    {
        ((TestDialog)dialog).DataContext = viewModel;

        var dialogTaskRunner = new TaskRunner(busy => _events.Add(new BusyChangedEvent(viewModel, busy)));
        _dialogTaskRunners.Add(dialogTaskRunner);
        taskRunner = dialogTaskRunner;
    }

    /// <inheritdoc/>
    protected override void StartShowingDialog(object dialog)
    {
        var viewModel = ((TestDialog)dialog).DataContext!;
        var parentViewModel = TopDialog;

        _showingDialogs.Add(viewModel);
        _events.Add(new DialogShownEvent(viewModel, parentViewModel));

        if (viewModel is MessageDialogViewModel messageDialog)
        {
            int buttonIndex;

            if (_messageDialogScript is not null)
            {
                buttonIndex = _messageDialogScript(messageDialog);
            }
            else if (AutoAcceptMessageDialogs)
            {
                buttonIndex = messageDialog.DefaultButtonIndex >= 0 ? messageDialog.DefaultButtonIndex : 0;
            }
            else
            {
                FailTest(new InvalidOperationException(
                    $"Unexpected message dialog (title: '{messageDialog.Title}', message: '{messageDialog.Message}'). " +
                    "Script a response with OnMessageDialog() or set AutoAcceptMessageDialogs to true."));

                return;
            }

            RunScript(() => {
                messageDialog.OnButtonClick(buttonIndex);
                return Task.CompletedTask;
            });
        }
        else if (FindDialogScript(viewModel.GetType()) is { } script)
        {
            RunScript(() => script(viewModel));
        }
    }

    /// <inheritdoc/>
    protected override void HideDialog(object dialog)
    {
        var viewModel = ((TestDialog)dialog).DataContext!;
        _showingDialogs.Remove(viewModel);
        _events.Add(new DialogClosedEvent(viewModel));
    }

    /// <inheritdoc/>
    protected override object? CreateDefaultDialog(Type viewModelType) => new TestDialog();

    /// <inheritdoc/>
    protected override void OnLayerCoveredChanged(object? dialog, bool isCovered)
    {
        _events.Add(new LayerCoveredEvent((dialog as TestDialog)?.DataContext, isCovered));
    }

    /// <inheritdoc/>
    protected override void RestoreDialogFocusState(object dialog, object? focusState)
    {
        _events.Add(new DialogFocusRestoredEvent(((TestDialog)dialog).DataContext!));
    }

    /// <inheritdoc/>
    protected override void OnViewModelLifecycleInvoking(IRoutedViewModelBase viewModel, ViewModelLifecycleStage stage)
    {
        _events.Add(new ViewModelLifecycleEvent(viewModel, stage));
    }

    /// <inheritdoc/>
    protected override void OnNavigationRedirecting(IRoutedViewModelBase viewModel, Redirect redirect)
    {
        _events.Add(new NavigationRedirectedEvent(viewModel, redirect));
    }

    /// <inheritdoc/>
    protected override object? OnNavigationStarting(NavigationType navigationType, NavigatorRoute targetRoute)
    {
        _events.Add(new NavigationStartedEvent(navigationType, targetRoute));
        return null;
    }

    /// <inheritdoc/>
    protected override void OnNavigationCompleted(NavigationType navigationType, NavigatorRoute targetRoute, NavigationResult result, object? state)
    {
        LastNavigationResult = result;
        _events.Add(new NavigationCompletedEvent(navigationType, targetRoute, result));
    }

    private static TestNavigatorBuilder CreateBuilder(Action<TestNavigatorBuilder> buildAction)
    {
        var builder = new TestNavigatorBuilder();
        buildAction(builder);
        return builder;
    }

    private Func<IDialogViewModel, Task>? FindDialogScript(Type viewModelType)
    {
        if (_dialogScripts.TryGetValue(viewModelType, out var script))
            return script;

        return _dialogScripts.FirstOrDefault(pair => pair.Key.IsAssignableFrom(viewModelType)).Value;
    }

    /// <summary>
    /// Runs a dialog script asynchronously on the test context so it executes after the code that showed the dialog is awaiting it. Script failures
    /// are rethrown on the test context, which fails the test.
    /// </summary>
    private static void RunScript(Func<Task> script)
    {
        var context = SynchronizationContext.Current!;

        context.Post(async _ => {
            try
            {
                await script();
            }
            catch (Exception ex)
            {
                FailTest(ex);
            }
        }, null);
    }

    /// <summary>
    /// Fails the running test by throwing the exception from the test context's queue, which surfaces it from <see cref="NavigationTestContext.Run(Func{Task})"/>
    /// even if the code under test is still awaiting something.
    /// </summary>
    private static void FailTest(Exception exception)
    {
        var info = ExceptionDispatchInfo.Capture(exception);
        SynchronizationContext.Current!.Post(static state => ((ExceptionDispatchInfo)state!).Throw(), info);
    }

    private sealed class BusyRelay
    {
        public Action<bool>? Handler { get; set; }

        public void Invoke(bool busy) => Handler?.Invoke(busy);
    }
}
