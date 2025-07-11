using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation.InternalServices;

internal static class MixinManager
{
    private static readonly ConditionalWeakTable<IDialogViewModel, IDialogNavigator> _viewModelToDialogNavigatorTable = [];
    private static readonly ConditionalWeakTable<IRoutedViewModelBase, INavigator> _viewModelToNavigatorTable = [];
    private static readonly ConditionalWeakTable<IRoutedViewModelBase, object> _viewModelToParameterTable = [];

    public static IDialogNavigator? GetDialogNavigator(IDialogViewModel viewModel)
    {
        return _viewModelToDialogNavigatorTable.TryGetValue(viewModel, out var dialogNavigator) ? dialogNavigator : null;
    }

    public static void SetDialogNavigator(IDialogViewModel viewModel, IDialogNavigator dialogNavigator)
    {
        if (!_viewModelToDialogNavigatorTable.TryAdd(viewModel, dialogNavigator))
            throw new InvalidOperationException("A different dialog navigator for this view model already exists.");
    }

    public static INavigator? GetNavigator(IRoutedViewModelBase viewModel)
    {
        return _viewModelToNavigatorTable.TryGetValue(viewModel, out var navigator) ? navigator : null;
    }

    public static void SetNavigator(IRoutedViewModelBase viewModel, INavigator navigator)
    {
        if (!_viewModelToNavigatorTable.TryAdd(viewModel, navigator))
            throw new InvalidOperationException("A different navigator for this view model already exists.");
    }

    public static bool TryGetParameter<T>(IRoutedViewModel<T> viewModel, [MaybeNullWhen(false)] out T parameter) where T : notnull
    {
        if (_viewModelToParameterTable.TryGetValue(viewModel, out object? parameterBox))
        {
            if (parameterBox is ParameterBox<T> box)
            {
                parameter = box.Value;
                return true;
            }
        }

        parameter = default;
        return false;
    }

    public static void SetParameter<T>(IRoutedViewModel<T> viewModel, T parameter) where T : notnull
    {
        if (!_viewModelToParameterTable.TryAdd(viewModel, new ParameterBox<T>(parameter)))
            throw new InvalidOperationException("A different parameter for this view model already exists.");
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
