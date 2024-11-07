using Microsoft.UI.Dispatching;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a control.
/// </summary>
public abstract class ViewNavigatorBase<TNavControl>(TNavControl navControl) : IViewNavigator where TNavControl : UIElement
{
    /// <summary>
    /// Gets the navigation control that the navigator is managing the active view for.
    /// </summary>
    protected TNavControl NavControl { get; } = navControl;

    /// <summary>
    /// Sets the active view for the control.
    /// </summary>
    protected abstract void SetActiveView(UIElement? view);

    #region Explicit Interface Implementations

    /// <inheritdoc/>
    DispatcherQueue IViewNavigator.DispatcherQueue => NavControl.DispatcherQueue ?? throw new InvalidOperationException("Dispatcher queue is not available.");

    /// <inheritdoc/>
    XamlRoot? IViewNavigator.XamlRoot => NavControl.XamlRoot;

    /// <inheritdoc/>
    void IViewNavigator.SetActiveView(UIElement? view) => SetActiveView(view);

    #endregion
}
