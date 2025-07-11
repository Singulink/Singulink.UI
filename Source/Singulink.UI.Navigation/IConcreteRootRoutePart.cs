namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a root route part, either with no parameters or with all its parameters resolved.
/// </summary>
public interface IConcreteRootRoutePart<TViewModel> : IConcreteRootRoutePart
    where TViewModel : class
{
}

/// <summary>
/// Represents a root route part, either with no parameters or with all its parameters resolved.
/// </summary>
public interface IConcreteRootRoutePart : IConcreteRoutePart
{
}
