namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.CurrentRouteStartsWith{TRootViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel})"/>
    public bool CurrentRouteStartsWith<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart)
        where TRootViewModel : class
    {
        EnsureThreadAccess();
        return CurrentRouteStartsWith([rootRoutePart, childRoutePart]);
    }

    /// <inheritdoc cref="INavigator.CurrentRouteStartsWith{TRootViewModel, TChildViewModel1}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1})"/>
    public bool CurrentRouteStartsWith<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        EnsureThreadAccess();
        return CurrentRouteStartsWith([rootRoutePart, childRoutePart1, childRoutePart2]);
    }

    /// <inheritdoc cref="INavigator.CurrentRouteStartsWith{TRootViewModel, TChildViewModel1, TChildViewModel2}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2})"/>
    public bool CurrentRouteStartsWith<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        EnsureThreadAccess();
        return CurrentRouteStartsWith([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3]);
    }

    private bool CurrentRouteStartsWith(List<IConcreteRoutePart> routeParts)
    {
        string current = CurrentRouteInternal?.Path;

        if (current is null)
            return false;

        string partial = GetPath(routeParts);

        if (!current.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            return false;

        return partial.Length == current.Length || current[partial.Length] == '/';
    }
}
