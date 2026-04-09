using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation.Utilities;

internal abstract class RouteParamsHandler
{
    /// <summary>
    /// The key used for single-value (non-model) parameters in <see cref="RouteValuesCollection"/>.
    /// </summary>
    internal const string SingleParamKey = "*";

    /// <summary>
    /// Gets a value indicating whether this handler is for a params model type (<see cref="IRouteParamsModel{TSelf}"/>) vs a single value.
    /// </summary>
    public abstract bool IsParamsModel { get; }

    /// <summary>
    /// Gets the names of required parameters, or an empty list for non-model handlers.
    /// </summary>
    public abstract IReadOnlyList<string> RequiredParameterNames { get; }

    /// <summary>
    /// Gets a value indicating whether this handler provides query string access (i.e. the parameter type consumes query string entries).
    /// </summary>
    public abstract bool ProvidesQueryAccess { get; }
}

internal abstract class RouteParamsHandler<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T> : RouteParamsHandler
    where T : notnull
{
    private static readonly RouteParamsHandler<T>? _instance = Create();

    public static RouteParamsHandler<T> Instance
    {
        get {
            if (_instance is null)
                ThrowUnsupportedType();

            return _instance;
        }
    }

    /// <summary>
    /// Builds <typeparamref name="T"/> from the parsed path holes + query string (for URL matching).
    /// </summary>
    public abstract bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out T value);

    /// <summary>
    /// Converts <typeparamref name="T"/> to a <see cref="RouteValuesCollection"/> (for URL generation).
    /// </summary>
    public abstract RouteValuesCollection ToRouteValues(T parameter);

    [DoesNotReturn]
    private static void ThrowUnsupportedType() => throw new NotSupportedException(
        $"Type '{typeof(T)}' is not supported as a route parameter type. " +
        $"Allowed parameter types include strings, primitive types, types that implement IParsable<T> or ISingleRouteParam<T>, " +
        $"and types that implement IRouteParamsModel<T> (or annotated with [RouteParamsModel]).");

    private static RouteParamsHandler<T>? Create()
    {
        if (typeof(T) == typeof(object))
            return null;

        if (typeof(T) == typeof(RouteQuery))
            return (RouteParamsHandler<T>)(object)new RouteQueryHandler();

        Type handlerType;

        if (IsParamsModelType())
        {
            handlerType = typeof(ParamsModelHandler<>).MakeGenericType(typeof(T));
        }
        else if (IsSingleParamType())
        {
            handlerType = typeof(SingleParamHandler<>).MakeGenericType(typeof(T));
        }
        else if (IsParsableType())
        {
            handlerType = typeof(ParsableParamHandler<>).MakeGenericType(typeof(T));
        }
        else
        {
            return null;
        }

        try
        {
            return (RouteParamsHandler<T>)Activator.CreateInstance(handlerType)!;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to create RouteParamHandler for type '{typeof(T)}': {ex}");
            return null;
        }
    }

#pragma warning disable IL2090
    // DAM.Interfaces is not needed, IRouteParamsModel<T> is preserved with AOT-safe typeof(ParamsModelHandler<>).MakeGenericType()
    private static bool IsParamsModelType() => typeof(T).GetInterfaces().Any(i =>
        i.IsGenericType &&
        i.GetGenericTypeDefinition() == typeof(IRouteParamsModel<>) &&
        i.GetGenericArguments()[0] == typeof(T));

    // DAM.Interfaces is not needed, ISingleRouteParam<T> is preserved with AOT-safe typeof(SingleParamHandler<>).MakeGenericType()
    private static bool IsSingleParamType() => typeof(T).GetInterfaces().Any(i =>
        i.IsGenericType &&
        i.GetGenericTypeDefinition() == typeof(ISingleRouteParam<>) &&
        i.GetGenericArguments()[0] == typeof(T));

    // DAM.Interfaces is not needed, IParsable<T> is preserved with AOT-safe typeof(ParsableParamHandler<>).MakeGenericType()
    private static bool IsParsableType() => typeof(T).GetInterfaces().Any(i =>
        i.IsGenericType &&
        i.GetGenericTypeDefinition() == typeof(IParsable<>) &&
        i.GetGenericArguments()[0] == typeof(T));
#pragma warning restore IL2090
}

file sealed class ParsableParamHandler<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T>
    : RouteParamsHandler<T> where T : notnull, IParsable<T>
{
    public override bool IsParamsModel => false;

    public override IReadOnlyList<string> RequiredParameterNames => [SingleParamKey];

    public override bool ProvidesQueryAccess => false;

    public override bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out T value)
    {
        return values.TryConsume(SingleParamKey, out value);
    }

    public override RouteValuesCollection ToRouteValues(T parameter)
    {
        return new RouteValuesCollection(1) {
            { SingleParamKey, parameter },
        };
    }
}

file sealed class SingleParamHandler<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T>
    : RouteParamsHandler<T> where T : notnull, ISingleRouteParam<T>
{
    public override bool IsParamsModel => false;

    public override IReadOnlyList<string> RequiredParameterNames => [SingleParamKey];

    public override bool ProvidesQueryAccess => false;

    public override bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out T value)
    {
        if (values.TryConsumeValue(SingleParamKey, out string? str) && T.TryParse(str, out value))
            return true;

        value = default;
        return false;
    }

    public override RouteValuesCollection ToRouteValues(T parameter)
    {
        return new RouteValuesCollection(1) {
            { SingleParamKey, parameter.ToString() ?? string.Empty },
        };
    }
}

file sealed class ParamsModelHandler<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T> : RouteParamsHandler<T> where T : notnull, IRouteParamsModel<T>
{
    public override bool IsParamsModel => true;

    public override IReadOnlyList<string> RequiredParameterNames => T.RequiredParameterNames;

    public override bool ProvidesQueryAccess => T.ProvidesRemainingQueryAccess;

    public override bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out T value)
    {
        try
        {
            return T.TryCreate(values, out value);
        }
        catch (Exception ex)
        {
            throw new NavigationRouteException($"An error occurred while creating route parameters model of type '{typeof(T)}': {ex.Message}", ex);
        }
    }

    public override RouteValuesCollection ToRouteValues(T parameter)
    {
        try
        {
            return parameter.ToRouteValues();
        }
        catch (Exception ex)
        {
            throw new NavigationRouteException($"An error occurred while building route values from model type '{typeof(T)}': {ex.Message}", ex);
        }
    }
}

file sealed class RouteQueryHandler : RouteParamsHandler<RouteQuery>
{
    public override bool IsParamsModel => false;

    public override IReadOnlyList<string> RequiredParameterNames => [];

    public override bool ProvidesQueryAccess => true;

    public override bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out RouteQuery value)
    {
        value = values.ConsumeQuery();
        return true;
    }

    public override RouteValuesCollection ToRouteValues(RouteQuery parameter)
    {
        var values = new RouteValuesCollection(parameter.Count);
        values.AddQuery(parameter);
        return values;
    }
}
