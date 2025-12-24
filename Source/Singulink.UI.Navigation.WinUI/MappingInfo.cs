using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a mapping between a view model type and a view type.
/// </summary>
internal sealed class MappingInfo
{
    private readonly ConstructorInfo _vmCtor;
    private readonly ImmutableArray<(ParameterInfo Param, bool IsNullable)> _vmCtorParams;

    /// <summary>
    /// Gets the view model type.
    /// </summary>
    // Note: NonPublicConstructors is needed for calling GetUninitializedObject() in CreateViewModel().
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    public Type ViewModelType { get; init; }

    /// <summary>
    /// Gets the view type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type ViewType { get; init; }

    private MappingInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type viewModelType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type viewType)
    {
        ViewModelType = viewModelType;
        ViewType = viewType;

        var ctorCandidates = viewModelType.GetConstructors();

        if (ctorCandidates.Length is 0)
            throw new InvalidOperationException($"View model type '{viewModelType}' does not have any public constructors.");

        if (ctorCandidates.Length > 1)
            throw new InvalidOperationException($"View model type '{viewModelType}' has multiple public constructors.");

        _vmCtor = ctorCandidates[0];
        _vmCtorParams = [.. _vmCtor.GetParameters().Select(p => (p, new NullabilityInfoContext().Create(p).WriteState is NullabilityState.Nullable))];

        if (_vmCtorParams.Any(p => p.Param.ParameterType.IsByRef))
            throw new InvalidOperationException($"View model type '{viewModelType}' has a constructor with by-ref parameters, which is not supported.");
    }

    /// <summary>
    /// Creates a new mapping info instance.
    /// </summary>
    public static MappingInfo Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type viewModelType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type viewType)
    {
        return new(viewModelType, viewType);
    }

    /// <summary>
    /// Creates an instance of the view type.
    /// </summary>
    public FrameworkElement CreateView() => (FrameworkElement)Activator.CreateInstance(ViewType)!;

    /// <summary>
    /// Creates an instance of the view model type.
    /// </summary>
    public IRoutedViewModelBase CreateViewModel(INavigator navigator, ConcreteRoute.Item routeItem)
    {
        var viewModel = (IRoutedViewModelBase)RuntimeHelpers.GetUninitializedObject(ViewModelType);
        MixinManager.SetNavigator(viewModel, navigator);

        object routePartParameter = routeItem.ConcreteRoutePart.Parameter;

        if (routePartParameter is not null)
            MixinManager.SetParameter(viewModel, routePartParameter);

        object?[] args = new object?[_vmCtorParams.Length];

        for (int i = 0; i < _vmCtorParams.Length; i++)
        {
            var (param, isNullable) = _vmCtorParams[i];
            var paramType = param.ParameterType;

            args[i] = routeItem.ParentItem?.GetChildViewModelService(paramType) ?? navigator.RootServices.GetService(paramType);

            if (args[i] is null)
            {
                if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else if (!isNullable)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve required constructor parameter '{param.Name}' of type '{paramType}' for view model type '{ViewModelType}'.");
                }
            }
        }

        _vmCtor.Invoke(viewModel, args);

        return viewModel;
    }
}
