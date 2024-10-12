namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view that is associated with a view model and can be navigated to using a route.
/// </summary>
public interface IRoutedView<TViewModel> where TViewModel : IRoutedViewModelBase
{
    /// <summary>
    /// Gets the view model associated with the view.
    /// </summary>
    public TViewModel Model { get; }
}
