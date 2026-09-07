namespace IconPackBuilder.Core.Services;

/// <summary>
/// Describes the host the builder runs in, for the few places where the UI adapts to it.
/// </summary>
public interface IHostInfo
{
    /// <summary>
    /// Gets a note shown on the start screen explaining how projects and exports work in this host, or <see langword="null"/> if none is needed.
    /// </summary>
    public string? StartPageNote { get; }

    /// <summary>
    /// Gets a value indicating whether the app can be exited from its own menu. A browser tab cannot close itself.
    /// </summary>
    public bool CanExit { get; }
}

/// <summary>
/// Host information for the desktop app, which needs no explanation.
/// </summary>
public sealed class DefaultHostInfo : IHostInfo
{
    public static DefaultHostInfo Instance { get; } = new();

    public string? StartPageNote => null;

    public bool CanExit => true;
}
