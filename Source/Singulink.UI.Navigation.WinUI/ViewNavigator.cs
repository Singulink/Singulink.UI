namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a view navigator that sets the active view displayed in a control.
/// </summary>
public abstract class ViewNavigator
{
    /// <summary>
    /// Creates a new instance of the <see cref="ViewNavigator"/> class using the specified content control.
    /// </summary>
    public static ContentControlNavigator Create(ContentControl contentControl) => new(contentControl);

    /// <summary>
    /// Creates a new <see cref="ViewNavigator"/> instance for the specified window.
    /// </summary>
    public static ViewNavigator Create(Window window)
    {
        var contentControl = new ContentControl {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        window.Content = contentControl;

        return new ContentControlNavigator(contentControl);
    }

    /// <summary>
    /// Gets the navigation control that the navigator is managing the active view for.
    /// </summary>
    public abstract Control NavigationControl { get; }

    /// <summary>
    /// Sets the active view for the control.
    /// </summary>
    protected internal abstract void SetActiveView(FrameworkElement? view);
}
