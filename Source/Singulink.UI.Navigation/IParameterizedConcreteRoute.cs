namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a parameterized concrete root or nested route.
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
