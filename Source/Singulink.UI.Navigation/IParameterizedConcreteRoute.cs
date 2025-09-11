namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a parameterized route with all its parameters resolved.
/// </summary>
public interface IParameterizedConcreteRoute<TViewModel, TParam> : IConcreteRoutePart
    where TViewModel : IRoutedViewModel<TParam>
    where TParam : notnull
{
    /// <summary>
    /// Gets the parameter for the concrete route.
    /// </summary>
    public new TParam Parameter { get; }
}
