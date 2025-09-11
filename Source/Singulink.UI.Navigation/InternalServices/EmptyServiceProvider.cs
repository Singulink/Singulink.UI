namespace Singulink.UI.Navigation.InternalServices;

/// <summary>
/// Empty service provider that always returns <see langword="null"/> for any service type.
/// </summary>
public sealed partial class EmptyServiceProvider : IServiceProvider
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="EmptyServiceProvider"/>.
    /// </summary>
    public static EmptyServiceProvider Instance { get; } = new();

    private EmptyServiceProvider() { }

    /// <summary>
    /// Gets a service object of the specified type. Always returns <see langword="null"/>.
    /// </summary>
    public object? GetService(Type serviceType) => null;
}
