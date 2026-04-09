#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// A plain non-parent fake view. The navigator only needs an instance — view content rendering is irrelevant in unit tests.
/// </summary>
public class FakeView
{
    public IRoutedViewModelBase? DataContext { get; set; }
}

/// <summary>
/// Marker interface for fake parent views that should provide a child <see cref="TestViewNavigator"/> when wired.
/// </summary>
public interface IFakeParentView
{
}

/// <summary>
/// A fake parent view that exposes a <see cref="TestViewNavigator"/> so the navigator can host child views beneath it.
/// </summary>
public class FakeParentView : FakeView, IFakeParentView
{
    public TestViewNavigator ChildNavigator { get; } = new();
}

/// <summary>
/// Tracks "active view" assignments coming from <see cref="NavigatorCore.SetActiveView"/>.
/// </summary>
public class TestViewNavigator
{
    public object? ActiveView { get; private set; }

    public List<object?> History { get; } = [];

    internal void SetActiveView(object? view)
    {
        ActiveView = view;
        History.Add(view);
    }
}
