using Microsoft.UI.Dispatching;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a control.
/// </summary>
public interface IViewNavigator
{
    internal XamlRoot? XamlRoot { get; }

    internal DispatcherQueue DispatcherQueue { get; }

    internal void SetActiveView(UIElement? view);
}
