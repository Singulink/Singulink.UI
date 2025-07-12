namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a parent view that can navigate to child views.
/// </summary>
public interface IParentView
{
    /// <summary>
    /// Gets the view navigator that is responsible for managing the active child view.
    /// </summary>
    public ViewNavigator CreateChildViewNavigator();
}
