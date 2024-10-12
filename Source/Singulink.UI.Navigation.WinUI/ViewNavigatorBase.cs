using Microsoft.UI.Dispatching;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a control.
/// </summary>
public abstract class ViewNavigatorBase<TTargetControl> : IViewNavigator where TTargetControl : UIElement
{
    private readonly Func<TTargetControl> _getTargetControlFunc;
    private TTargetControl? _targetControl;

    /// <summary>
    /// Gets the target control that the navigator is managing the active view for.
    /// </summary>
    protected TTargetControl TargetControl => _targetControl ??= _getTargetControlFunc();

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewNavigatorBase{TTargetControl}"/> class using the specified function to get the target control.
    /// </summary>
    public ViewNavigatorBase(Func<TTargetControl> getTargetControlFunc)
    {
        _getTargetControlFunc = getTargetControlFunc;
    }

    /// <summary>
    /// Sets the active view for the control.
    /// </summary>
    protected abstract void SetActiveView(UIElement view);

    #region Explicit Interface Implementations

    /// <inheritdoc/>
    DispatcherQueue IViewNavigator.DispatcherQueue => TargetControl.DispatcherQueue;

    /// <inheritdoc/>
    XamlRoot IViewNavigator.XamlRoot => TargetControl.XamlRoot ?? throw new InvalidOperationException("XAML root is not available.");

    /// <inheritdoc/>
    void IViewNavigator.SetActiveView(UIElement view) => SetActiveView(view);

    #endregion
}
