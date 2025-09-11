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

        _vmCtor = viewModelType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault() ??
            throw new InvalidOperationException($"View model type '{viewModelType}' must have a public constructor.");

        var nullabilityContext = new NullabilityInfoContext();

        _vmCtorParams = _vmCtor.GetParameters()
            .Select(p => (p, nullabilityContext.Create(p).WriteState is NullabilityState.Nullable))
            .ToImmutableArray();
    }

    /// <summary>
    /// Creates a new mapping info instance.
    /// </summary>
    public static MappingInfo Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type viewModelType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type viewType)
        => new(viewModelType, viewType);

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

            args[i] = routeItem.ParentItem?.GetChildViewModelService(paramType) ?? navigator.Services.GetService(paramType);

            if (args[i] is null)
            {
                if (param.HasDefaultValue)
                    args[i] = param.DefaultValue;
                else if (!isNullable)
                    throw new InvalidOperationException($"Cannot resolve required constructor parameter '{param.Name}' of type '{paramType}' for view model type '{ViewModelType}'.");
            }
        }

        _vmCtor.Invoke(viewModel, args);

        return viewModel;
    }
}
