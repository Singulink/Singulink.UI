namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a specified root or nested route with a parameter.
/// </summary>
public interface IParameterizedSpecifiedRoute<TParam, TViewModel>
    where TParam : notnull
    where TViewModel : IRoutedViewModelBase
{
    /// <summary>
    /// Gets the parameter for the specified route.
    /// </summary>
    public TParam Parameter { get; }
}
