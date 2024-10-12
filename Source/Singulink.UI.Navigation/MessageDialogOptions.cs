namespace Singulink.UI.Navigation;

/// <summary>
/// Options for configuring a message dialog.
/// </summary>
public sealed class MessageDialogOptions
{
    private int _defaultButtonIndex;
    private int _cancelButtonIndex;

    /// <summary>
    /// Gets or sets the title of the dialog.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets the message of the dialog.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the labels for the buttons displayed in the dialog.
    /// </summary>
    public IReadOnlyList<string> ButtonLabels { get; }

    /// <summary>
    /// Gets or sets the index of the button that is focused by default so it is clicked when the user presses the Enter or Space key and has an
    /// accent style applied to it. Defaults to <c>0</c> (the first button). A value of <c>-1</c> indicates that there should be no default button.
    /// </summary>
    public int DefaultButtonIndex
    {
        get => _defaultButtonIndex;
        set {
            if (value < -1 || value >= ButtonLabels.Count)
                throw new ArgumentOutOfRangeException(nameof(value), "Button index is out of range.");

            _defaultButtonIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets the index of the button that should be triggered when the user presses the Escape key. Defaults to the index of the last button. A value of
    /// <c>-1</c> indicates that there should be no cancel button.
    /// </summary>
    public int CancelButtonIndex
    {
        get => _cancelButtonIndex;
        set {
            if (value < -1 || value >= ButtonLabels.Count)
                throw new ArgumentOutOfRangeException(nameof(value), "Button index is out of range.");

            _cancelButtonIndex = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialogOptions"/> class.
    /// </summary>
    public MessageDialogOptions(string message, IEnumerable<string> buttonLabels)
    {
        Message = message;
        ButtonLabels = [.. buttonLabels];

        if (ButtonLabels.Count is 0)
            throw new ArgumentException("At least one button label must be provided.", nameof(buttonLabels));

        _cancelButtonIndex = ButtonLabels.Count - 1;
    }
}
