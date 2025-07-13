using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Provides methods for associating navigators and parameters with view models.
/// </summary>
/// <remarks>
/// This class is used internally by UI framework-specific navigator implementations to associate navigators and parameters with view models, but it can also be
/// used in view model tests to associate mocked navigators with view models and provide parameters to view models before the test calls <see
/// cref="IRoutedViewModelBase.OnNavigatedToAsync(Singulink.UI.Navigation.NavigationArgs)"/>.
/// </remarks>
public static class MixinManager
{
    private static readonly ConditionalWeakTable<IDialogViewModel, IDialogNavigator> _viewModelToDialogNavigatorTable = [];
    private static readonly ConditionalWeakTable<IRoutedViewModelBase, INavigator> _viewModelToNavigatorTable = [];
    private static readonly ConditionalWeakTable<IRoutedViewModelBase, object> _viewModelToParameterTable = [];

    /// <summary>
    /// Returns the dialog navigator associated with the specified view model.
    /// </summary>
    public static IDialogNavigator? GetDialogNavigator(IDialogViewModel viewModel)
    {
        return _viewModelToDialogNavigatorTable.TryGetValue(viewModel, out var dialogNavigator) ? dialogNavigator : null;
    }

    /// <summary>
    /// Associates a dialog navigator with the specified view model.
    /// </summary>
    /// <exception cref="InvalidOperationException">A dialog navigator has already been associated with the view model.</exception>
    public static void SetDialogNavigator(IDialogViewModel viewModel, IDialogNavigator dialogNavigator)
    {
        if (!_viewModelToDialogNavigatorTable.TryAdd(viewModel, dialogNavigator))
            throw new InvalidOperationException("A dialog navigator has already been associated with the view model.");
    }

    /// <summary>
    /// Returns the navigator associated with the specified view model.
    /// </summary>
    public static INavigator? GetNavigator(IRoutedViewModelBase viewModel)
    {
        return _viewModelToNavigatorTable.TryGetValue(viewModel, out var navigator) ? navigator : null;
    }

    /// <summary>
    /// Associates a navigator with the specified view model.
    /// </summary>
    /// <exception cref="InvalidOperationException">A navigator has already been associated with the view model.</exception>
    public static void SetNavigator(IRoutedViewModelBase viewModel, INavigator navigator)
    {
        if (!_viewModelToNavigatorTable.TryAdd(viewModel, navigator))
            throw new InvalidOperationException("A navigator has already been associated with the view model.");
    }

    /// <summary>
    /// Gets the parameter associated with the specified view model.
    /// </summary>
    public static bool TryGetParameter<T>(IRoutedViewModel<T> viewModel, [MaybeNullWhen(false)] out T parameter) where T : notnull
    {
        if (_viewModelToParameterTable.TryGetValue(viewModel, out object parameterObj))
        {
            if (parameterObj is ParameterBox<T> box)
            {
                parameter = box.Value;
                return true;
            }
        }

        parameter = default;
        return false;
    }

    /// <summary>
    /// Associates a parameter with the specified view model.
    /// </summary>
    /// <exception cref="InvalidOperationException">A parameter has already been associated with the view model.</exception>
    public static void SetParameter<T>(IRoutedViewModel<T> viewModel, T parameter) where T : notnull
    {
        if (!_viewModelToParameterTable.TryAdd(viewModel, new ParameterBox<T>(parameter)))
            throw new InvalidOperationException("A parameter has already been associated with the view model.");
    }

    private class ParameterBox<T>
    {
        public T Value { get; }

        public ParameterBox(T value)
        {
            Value = value;
        }
    }
}
