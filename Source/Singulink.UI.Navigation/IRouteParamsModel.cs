using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a model that aggregates multiple route parameters and supports conversion to and from a <see cref="RouteValuesCollection"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRouteParamsModel
{
    /// <summary>
    /// Converts the current instance to a <see cref="RouteValuesCollection"/>.
    /// </summary>
    RouteValuesCollection ToRouteValues();
}

/// <summary>
/// Represents a model that aggregates multiple route parameters and supports conversion to and from a <see cref="RouteValuesCollection"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface IRouteParamsModel<TSelf> : IRouteParamsModel where TSelf : IRouteParamsModel<TSelf>
{
    /// <summary>
    /// Gets the names of required parameters. A view model's route does not match if any required parameters are missing.
    /// </summary>
    static abstract IReadOnlyList<string> RequiredParameterNames { get; }

    /// <summary>
    /// Gets a value indicating whether this model provides access to remaining (unconsumed) query string parameters.
    /// </summary>
    static abstract bool ProvidesRemainingQueryAccess { get; }

    /// <summary>
    /// Attempts to create an instance from the given <see cref="RouteValuesCollection"/>. Returns <see langword="false"/> if required parameters are
    /// missing, indicating the view model's route does not match.
    /// </summary>
    static abstract bool TryCreate(RouteValuesCollection values, [MaybeNullWhen(false)] out TSelf result);
}
