using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Constructor information for dialog view model types created through <see cref="IDialogPresenter.CreateDialogViewModel{TViewModel}(object?[])"/>.
/// Explicit arguments are matched to constructor parameters by type (positionally among parameters of the same type) and the remaining parameters are
/// resolved as services.
/// </summary>
internal sealed class DialogActivationInfo
{
    private static readonly ConcurrentDictionary<Type, DialogActivationInfo> Cache = new();

    [DynamicallyAccessedMembers(DAM.AllCtors)]
    private readonly Type _viewModelType;
    private readonly ConstructorInfo _ctor;
    private readonly ImmutableArray<(ParameterInfo Param, bool IsNullable)> _ctorParams;

    private DialogActivationInfo([DynamicallyAccessedMembers(DAM.AllCtors)] Type viewModelType)
    {
        _viewModelType = viewModelType;

        var ctorCandidates = viewModelType.GetConstructors();

        if (ctorCandidates.Length is 0)
            throw new InvalidOperationException($"Dialog view model type '{viewModelType}' does not have any public constructors.");

        if (ctorCandidates.Length > 1)
            throw new InvalidOperationException($"Dialog view model type '{viewModelType}' has multiple public constructors.");

        _ctor = ctorCandidates[0];
        _ctorParams = [.. _ctor.GetParameters().Select(p => (p, new NullabilityInfoContext().Create(p).WriteState is NullabilityState.Nullable))];

        if (_ctorParams.Any(p => p.Param.ParameterType.IsByRef))
            throw new InvalidOperationException($"Dialog view model type '{viewModelType}' has a constructor with by-ref parameters, which is not supported.");
    }

    public static DialogActivationInfo Get([DynamicallyAccessedMembers(DAM.AllCtors)] Type viewModelType)
    {
        if (Cache.TryGetValue(viewModelType, out var info))
            return info;

        // Constructed from the annotated parameter (rather than a cache factory receiving the key) so the constructors are preserved for trimming.
        info = new DialogActivationInfo(viewModelType);
        return Cache.GetOrAdd(viewModelType, info);
    }

    /// <summary>
    /// Allocates an uninitialized instance so that mixins (e.g. the navigator) can be associated before the constructor runs.
    /// </summary>
    public IDialogViewModel AllocateUninitialized() => (IDialogViewModel)RuntimeHelpers.GetUninitializedObject(_viewModelType);

    /// <summary>
    /// Resolves the constructor arguments from the explicit arguments and the service resolver.
    /// </summary>
    public object?[] ResolveArguments(object?[] explicitArgs, Func<Type, object?> resolveService)
    {
        for (int i = 0; i < explicitArgs.Length; i++)
        {
            if (explicitArgs[i] is null)
            {
                throw new ArgumentException(
                    $"Explicit argument {i} for dialog view model type '{_viewModelType}' is null. Null arguments cannot be matched to a constructor " +
                    "parameter by type; omit the argument and give the parameter a default value instead.", nameof(explicitArgs));
            }
        }

        object?[] args = new object?[_ctorParams.Length];
        bool[] explicitArgUsed = new bool[explicitArgs.Length];

        // Explicit arguments first: each parameter takes the first unused explicit argument assignable to it, so arguments of the same type match
        // positionally.
        for (int i = 0; i < _ctorParams.Length; i++)
        {
            var paramType = _ctorParams[i].Param.ParameterType;

            for (int j = 0; j < explicitArgs.Length; j++)
            {
                if (!explicitArgUsed[j] && paramType.IsInstanceOfType(explicitArgs[j]))
                {
                    args[i] = explicitArgs[j];
                    explicitArgUsed[j] = true;
                    break;
                }
            }
        }

        for (int j = 0; j < explicitArgs.Length; j++)
        {
            if (!explicitArgUsed[j])
            {
                throw new ArgumentException(
                    $"Explicit argument {j} of type '{explicitArgs[j]!.GetType()}' does not match any remaining constructor parameter of dialog view model " +
                    $"type '{_viewModelType}'.", nameof(explicitArgs));
            }
        }

        // Remaining parameters are services.
        for (int i = 0; i < _ctorParams.Length; i++)
        {
            if (args[i] is not null)
                continue;

            var (param, isNullable) = _ctorParams[i];
            var paramType = param.ParameterType;

            args[i] = resolveService(paramType);

            if (args[i] is null)
            {
                if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else if (!isNullable)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve required constructor parameter '{param.Name}' of type '{paramType}' for dialog view model type '{_viewModelType}'. " +
                        "Pass it as an explicit argument or register it as a root or child service.");
                }
            }
        }

        return args;
    }

    /// <summary>
    /// Runs the constructor on an instance allocated by <see cref="AllocateUninitialized"/>.
    /// </summary>
    public void InvokeConstructor(IDialogViewModel viewModel, object?[] args) => _ctor.Invoke(viewModel, args);
}
