namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Panel"/>.
/// </summary>
/// <remarks>
/// If the panel already contains the view then it will be made visible, otherwise the view will be added to the panel. All other children of the panel will be
/// hidden.
/// </remarks>
public class PanelNavigator : ViewNavigatorBase<Panel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PanelNavigator"/> class using the specified function to get the target panel.
    /// </summary>
    public PanelNavigator(Func<Panel> getTargetPanelFunc) : base(getTargetPanelFunc) { }

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement view)
    {
        var panel = TargetControl;

        foreach (var child in panel.Children)
        {
            if (child == view)
            {
                child.Visibility = Visibility.Visible;
                break;
            }

            child.Visibility = Visibility.Collapsed;
        }

        view.Visibility = Visibility.Visible;
        panel.Children.Add(view);
    }
}
