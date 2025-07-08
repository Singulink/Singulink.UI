namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view that is associated with a view model and can be navigated to using a route.
/// </summary>
public interface IRoutedView<TViewModel> : IRoutedViewBase where TViewModel : class, IRoutedViewModelBase
{
    /// <summary>
    /// Gets the view model associated with the view.
    /// </summary>
    public new TViewModel Model { get; }

    /// <inheritdoc/>
    IRoutedViewModelBase IRoutedViewBase.Model => Model;
}
