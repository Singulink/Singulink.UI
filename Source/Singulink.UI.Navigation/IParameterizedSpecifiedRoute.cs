namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a specified root or nested route with a parameter.
/// </summary>
public interface IParameterizedSpecifiedRoute<TParam, TViewModel> : ISpecifiedRoute
    where TParam : notnull
    where TViewModel : IRoutedViewModel<TParam>
{
    /// <summary>
    /// Gets the parameter for the specified route.
    /// </summary>
    public TParam Parameter { get; }
}
