namespace Singulink.UI.Navigation;

/// <summary>
/// Provides a set of predefined button labels for dialogs.
/// </summary>
public static class DialogButtonLabels
{
    /// <summary>
    /// Gets the "OK" button label.
    /// </summary>
    public static IReadOnlyList<string> OK { get; } = ["OK"];

    /// <summary>
    /// Gets the "OK" and "Cancel" button labels.
    /// </summary>
    public static IReadOnlyList<string> OKCancel { get; } = ["OK", "Cancel"];

    /// <summary>
    /// Gets the "Yes" and "No" button labels.
    /// </summary>
    public static IReadOnlyList<string> YesNo { get; } = ["Yes", "No"];

    /// <summary>
    /// Gets the "Yes", "No", and "Cancel" button labels.
    /// </summary>
    public static IReadOnlyList<string> YesNoCancel { get; } = ["Yes", "No", "Cancel"];
}
