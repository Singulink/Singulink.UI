namespace Singulink.UI.Navigation;

/// <content>
/// Provides handler related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.RegisterInitializeViewHandler{TView, TViewModel}(VVMAction{TView, TViewModel})"/>
    public void RegisterInitializeViewHandler<TView, TViewModel>(VVMAction<TView, TViewModel> handler)
        where TView : class
        where TViewModel : class
    {
        _initializeViewHandler += (viewObj, vmObj) => {
            if (viewObj is TView view && vmObj is TViewModel vm)
                handler(view, vm);
        };
    }

    /// <inheritdoc cref="INavigator.RegisterAsyncNavigationHandler(Action{Task})"/>
    public void RegisterAsyncNavigationHandler(Action<Task> handler)
    {
        EnsureThreadAccess();
        _asyncNavigationHandler += handler;
    }
}
