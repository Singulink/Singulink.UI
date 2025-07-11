namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a child route part, either with no parameters or with all its parameters resolved.
/// </summary>
public interface IConcreteChildRoutePart<TParentViewModel, TChildViewModel> : IConcreteChildRoutePart<TParentViewModel>
    where TParentViewModel : class
    where TChildViewModel : class
{
}

/// <summary>
/// Represents a child route part, either with no parameters or with all its parameters resolved.
/// </summary>
public interface IConcreteChildRoutePart<TParentViewModel> : IConcreteRoutePart
    where TParentViewModel : class
{
}
