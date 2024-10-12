namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Panel"/>.
/// </summary>
/// <remarks>
/// If the panel already contains the view then it will be made visible, otherwise the view will be added to the panel. All other children of the panel will be
/// hidden.
/// </remarks>
public class PanelNavigator(Panel navControl) : ViewNavigatorBase<Panel>(navControl)
{
    /// <inheritdoc/>
    protected override void SetActiveView(UIElement view)
    {
        var panel = NavControl;

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
