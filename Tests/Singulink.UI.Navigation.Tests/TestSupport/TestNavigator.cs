using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// In-process <see cref="NavigatorCore"/> for unit tests. Records all calls to the abstract surface so that tests can assert ordering and arguments.
/// </summary>
/// <remarks>
/// Must be constructed inside an <c>AsyncContext.Run(...)</c> block so that <see cref="TaskRunner"/> can capture a synchronization context.
/// </remarks>
public sealed class TestNavigator : NavigatorCore
{
    public TestNavigator(Action<TestNavigatorBuilder> buildAction)
        : this(new TestViewNavigator(), CreateBuilder(buildAction))
    {
    }

    private TestNavigator(TestViewNavigator rootViewNavigator, TestNavigatorBuilder builder)
        : base(rootViewNavigator, new TaskRunner(), builder)
    {
        RootViewNavigator = rootViewNavigator;
    }

    /// <summary>
    /// Gets the root <see cref="TestViewNavigator"/> that hosts the top-level view.
    /// </summary>
    public TestViewNavigator RootViewNavigator { get; }

    /// <summary>
    /// Gets the list of (view, viewModel) pairs in the order they were wired by <see cref="WireView"/>.
    /// </summary>
    public List<(object View, IRoutedViewModelBase ViewModel)> WiredViews { get; } = [];

    /// <summary>
    /// Gets the list of (dialog, viewModel) pairs in the order they were wired by <see cref="WireDialog"/>.
    /// </summary>
    public List<(object Dialog, IDialogViewModel ViewModel)> WiredDialogs { get; } = [];

    /// <summary>
    /// Gets the dialogs currently being shown, in the order they were started (top-most last).
    /// </summary>
    public List<object> ShownDialogs { get; } = [];

    /// <summary>
    /// Gets the full history of show/hide events, useful for asserting nested dialog ordering.
    /// </summary>
    public List<DialogEvent> DialogEvents { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="EnsureThreadAccess"/> should throw if it is invoked without an active synchronization context.
    /// </summary>
    public bool EnforceThreadAccess { get; set; }

    /// <summary>
    /// Exposes the protected <see cref="NavigatorCore.TryGetTopDialog"/> for tests.
    /// </summary>
    public new (DialogNavigatorCore Navigator, IDialogViewModel ViewModel)? TryGetTopDialog() => base.TryGetTopDialog();

    /// <inheritdoc />
    protected override void EnsureThreadAccess()
    {
        // Tests run inside AsyncContext.Run on a single thread, so any synchronization context is fine.
        if (EnforceThreadAccess && SynchronizationContext.Current is null)
            throw new InvalidOperationException("No synchronization context on the current thread.");
    }

    /// <inheritdoc />
    protected override bool CloseLightDismissPopups() => false;

    /// <inheritdoc />
    protected override void WireView(object view, IRoutedViewModelBase viewModel, out object? childViewNavigator)
    {
        WiredViews.Add((view, viewModel));

        if (view is FakeView fakeView)
            fakeView.DataContext = viewModel;

        childViewNavigator = (view as FakeParentView)?.ChildNavigator;
    }

    /// <inheritdoc />
    protected override void SetActiveView(object viewNavigator, object? view)
    {
        ((TestViewNavigator)viewNavigator).SetActiveView(view);
    }

    /// <inheritdoc />
    protected override void WireDialog(object dialog, IDialogViewModel viewModel, out ITaskRunner taskRunner)
    {
        WiredDialogs.Add((dialog, viewModel));

        if (dialog is FakeDialog fake)
            fake.DataContext = viewModel;

        taskRunner = new TaskRunner();
    }

    /// <inheritdoc />
    protected override void StartShowingDialog(object dialog)
    {
        ShownDialogs.Add(dialog);
        DialogEvents.Add(new DialogEvent(DialogEventKind.Show, dialog));
    }

    /// <inheritdoc />
    protected override void HideDialog(object dialog)
    {
        ShownDialogs.Remove(dialog);
        DialogEvents.Add(new DialogEvent(DialogEventKind.Hide, dialog));
    }

    private static TestNavigatorBuilder CreateBuilder(Action<TestNavigatorBuilder> buildAction)
    {
        var builder = new TestNavigatorBuilder();
        buildAction(builder);
        return builder;
    }
}

/// <summary>
/// Records a dialog show/hide event from the navigator.
/// </summary>
public readonly record struct DialogEvent(DialogEventKind Kind, object Dialog);

public enum DialogEventKind
{
    Show,
    Hide,
}
