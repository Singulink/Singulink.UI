namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a parent view that can navigate to nested child views.
/// </summary>
public interface IParentView
{
    /// <summary>
    /// Gets the view navigator that is responsible for managing the active nested view.
    /// </summary>
    public IViewNavigator CreateNestedViewNavigator();
}
