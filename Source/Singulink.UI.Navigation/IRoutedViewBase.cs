namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view that is associated with a view model and can be navigated to using a route.
/// </summary>
public interface IRoutedViewBase
{
    /// <summary>
    /// Gets the view model associated with the view.
    /// </summary>
    public IRoutedViewModelBase Model { get; }
}
