#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// Marker interface for the placeholder view type so it satisfies the navigator's parent view requirement for view models with child routes.
/// </summary>
internal interface ITestParentView
{
}

/// <summary>
/// Placeholder view that every routed view model is mapped to. It only carries the view model and a child view navigator so the navigator can host
/// child routes beneath it; nothing is rendered.
/// </summary>
internal sealed class TestParentView : ITestParentView
{
    public IRoutedViewModelBase? DataContext { get; set; }

    public TestViewNavigator ChildNavigator { get; } = new();
}

/// <summary>
/// Tracks the active placeholder view at one level of the view hierarchy.
/// </summary>
internal sealed class TestViewNavigator
{
    public TestParentView? ActiveView { get; set; }
}

/// <summary>
/// Placeholder dialog object that every dialog view model is mapped to.
/// </summary>
internal sealed class TestDialog
{
    public IDialogViewModel? DataContext { get; set; }
}
