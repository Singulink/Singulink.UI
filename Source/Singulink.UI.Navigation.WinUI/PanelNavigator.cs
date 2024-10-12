namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Panel"/>.
/// </summary>
/// <remarks>
/// If the panel already contains the view then it will be made visible, otherwise the view will be added to the panel. All other children of the panel will be
/// hidden.
/// </remarks>
public class PanelNavigator(Panel navControl, int maxCachedViews = 5) : ViewNavigatorBase<Panel>(navControl)
{
    private readonly int _maxCachedViews = maxCachedViews >= 0 ? maxCachedViews : throw new ArgumentOutOfRangeException(nameof(maxCachedViews));

    private readonly List<UIElement> _cachedViews = [];

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement? view)
    {
        var panel = NavControl;

        bool foundView = false;

        foreach (var child in panel.Children)
        {
            if (child == view)
            {
                child.Visibility = Visibility.Visible;
                foundView = true;
            }

            child.Visibility = Visibility.Collapsed;
        }

        if (view is not null)
        {
            if (!foundView)
            {
                view.Visibility = Visibility.Visible;
                panel.Children.Add(view);
                _cachedViews.Add(view);
            }
            else
            {
                _cachedViews.Remove(view);
                _cachedViews.Add(view);
            }

            if (_cachedViews.Count > _maxCachedViews)
            {
                var viewToRemove = _cachedViews[0];
                _cachedViews.RemoveAt(0);
                panel.Children.Remove(viewToRemove);
            }
        }
    }
}
