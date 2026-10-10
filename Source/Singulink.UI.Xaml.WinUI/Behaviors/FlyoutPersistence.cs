namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Specifies the changes of its window that a flyout stays open through. See <see cref="Flyouts.PersistOnProperty"/>.
/// </summary>
[Flags]
public enum FlyoutPersistence
{
    /// <summary>
    /// Indicates that the flyout closes for changes of its window as the framework has it do by default.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the flyout stays open when its window is deactivated, such as when the user switches to another window or tab, or the application
    /// shows a file picker. Calling <see cref="Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.Hide"/> while the window is still inactive closes it as usual.
    /// On Uno Platform this relies on light dismiss popups being dismissed after the <see cref="Microsoft.UI.Xaml.Window.Activated"/> handlers that were
    /// added before they opened, as in WinUI: in versions of Uno where the popups are dismissed first, the flyout still closes.
    /// </summary>
    WindowDeactivation = 1,

    /// <summary>
    /// Indicates that the flyout stays open when its window changes size, which mobile browsers also do to the page on their own (Edge on Android does
    /// when the soft keyboard is dismissed while it is still sliding in). WinUI closes the popup of the flyout directly for a size change, which cannot be
    /// canceled, so there the flyout is shown again at the same target once the size of the window has settled: its Closed and Opened events are raised
    /// for that, and the options it was first shown with are not kept. On Uno Platform a call to Hide that the application makes while the resize is still
    /// being laid out is ignored as well.
    /// </summary>
    WindowResize = 2,

    /// <summary>
    /// Indicates that the flyout stays open through all of the above.
    /// </summary>
    All = WindowDeactivation | WindowResize,
}
