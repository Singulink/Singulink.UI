namespace Singulink.UI.Navigation;

/// <summary>
/// View model for message dialogs shown by navigators. The default message dialog can be overridden by mapping a custom dialog to this view model type when
/// building an <see cref="INavigator"/>.
/// </summary>
public class MessageDialogViewModel
{
    private readonly IDialogNavigator _dialogNavigator;

    private int? _resultButtonIndex;

    /// <summary>
    /// Gets the title of the dialog.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the message of the dialog.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the index of the button that was clicked to close the dialog.
    /// </summary>
    public int ResultButtonIndex => _resultButtonIndex ?? throw new InvalidOperationException("Result is not available until the dialog is closed.");

    /// <summary>
    /// Gets the labels for the buttons displayed in the dialog.
    /// </summary>
    public IReadOnlyList<string> ButtonLabels { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialogViewModel"/> class.
    /// </summary>
    public MessageDialogViewModel(IDialogNavigator dialogNavigator, string message, string title, IEnumerable<string> buttonLabels)
    {
        _dialogNavigator = dialogNavigator;
        Message = message;
        Title = title;
        ButtonLabels = [.. buttonLabels];

        if (ButtonLabels.Count == 0)
            throw new ArgumentException("At least one button label must be provided.", nameof(buttonLabels));
    }

    /// <summary>
    /// Should be called when a button is clicked in the message dialog to set <see cref="ResultButtonIndex"/> and close the dialog.
    /// </summary>
    /// <param name="index">The index of the button that was clicked.</param>
    public void OnButtonClick(int index)
    {
        _resultButtonIndex = index;
        _dialogNavigator.Close();
    }
}
