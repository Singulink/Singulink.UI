using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Shorthand constants for <see cref="DynamicallyAccessedMemberTypes"/>.
/// </summary>
internal static class DAM
{
    internal const DynamicallyAccessedMemberTypes PublicDefaultCtor = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
    internal const DynamicallyAccessedMemberTypes AllCtors = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors;
}
