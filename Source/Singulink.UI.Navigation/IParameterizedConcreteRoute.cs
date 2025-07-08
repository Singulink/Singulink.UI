namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a concrete root or nested route with a resolved parameter (or parameter tuple, if there are multiple parameters).
/// </summary>
public interface IParameterizedConcreteRoute<TViewModel, TParam> : IConcreteRoute
    where TViewModel : IRoutedViewModel<TParam>
    where TParam : notnull
{
    /// <summary>
    /// Gets the parameter for the concrete route.
    /// </summary>
    public TParam Parameter { get; }
}
