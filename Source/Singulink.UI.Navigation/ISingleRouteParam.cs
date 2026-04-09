using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a type that can be used as a single view model route parameter. Implement this interface (instead of <see cref="IParsable{TSelf}"/>) on wrapper
/// types that should be usable only as a VM-level single parameter and not as a property in an <see cref="IRouteParamsModel{TSelf}"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface ISingleRouteParam<TSelf>
    where TSelf : ISingleRouteParam<TSelf>
{
    /// <summary>
    /// Attempts to parse the specified string into an instance of <typeparamref name="TSelf"/> using invariant culture formatting.
    /// </summary>
    static abstract bool TryParse(string s, [MaybeNullWhen(false)] out TSelf value);
}
