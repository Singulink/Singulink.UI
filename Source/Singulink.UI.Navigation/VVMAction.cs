namespace Singulink.UI.Navigation;

/// <summary>
/// Encapsulates a method that performs an action on a view and its associated view model.
/// </summary>
/// <typeparam name="TView">The type of the view.</typeparam>
/// <typeparam name="TViewModel">The type of the view model.</typeparam>
/// <param name="view">The view.</param>
/// <param name="viewModel">The view model.</param>
public delegate void VVMAction<TView, TViewModel>(TView view, TViewModel viewModel)
    where TView : class
    where TViewModel : class;
