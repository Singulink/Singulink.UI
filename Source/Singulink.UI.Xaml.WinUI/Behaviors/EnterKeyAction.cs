namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Specifies the action that should be taken when the Enter key is pressed on a control.
/// </summary>
public enum EnterKeyAction
{
    /// <summary>
    /// Indicates that no action should be taken when the Enter key is pressed.
    /// </summary>
    None,

    /// <summary>
    /// Indicates that the control should lose focus when the Enter key is pressed and any soft keyboards should be dismissed. This changes the Enter key label
    /// on soft keyboards to <c>"Done"</c> on iOS and Android.
    /// </summary>
    Done,

    /// <summary>
    /// Indicates that the next control in the tab order should be focused when the Enter key is pressed. This changes the Enter key label on soft keyboards to
    /// <c>"Next"</c> on iOS and Android.
    /// </summary>
    Next,
}
