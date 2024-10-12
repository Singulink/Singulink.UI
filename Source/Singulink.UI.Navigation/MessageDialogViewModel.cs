namespace Singulink.UI.Navigation;

/// <summary>
/// View model for message dialogs shown by navigators. The default message dialog can be overridden by mapping a custom dialog to this view model type
/// when building an <see cref="INavigator"/>.
/// </summary>
public sealed class MessageDialogViewModel : IDismissableDialogViewModel
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
    /// Gets the index of the button that is focused by default so it is clicked when the user presses the Enter or Space key. This button gets an accent style
    /// applied to it.
    /// </summary>
    public int DefaultButtonIndex { get; }

    /// <summary>
    /// Gets the index of the button that should be clicked when the user presses the Escape key.
    /// </summary>
    public int CancelButtonIndex { get; }

    /// <summary>
    /// Gets the index of the button that was clicked to close the dialog.
    /// </summary>
    /// <exception cref="InvalidOperationException">The dialog is still open.</exception>
    public int ResultButtonIndex => _resultButtonIndex ?? throw new InvalidOperationException("Result is not available until the dialog is closed.");

    /// <summary>
    /// Gets the labels for the buttons displayed in the dialog.
    /// </summary>
    public IReadOnlyList<string> ButtonLabels { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialogViewModel"/> class.
    /// </summary>
    internal MessageDialogViewModel(IDialogNavigator dialogNavigator, MessageDialogOptions options)
    {
        _dialogNavigator = dialogNavigator;
        Title = options.Title;
        Message = options.Message;
        ButtonLabels = options.ButtonLabels;
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

    /// <inheritdoc/>
    void IDismissableDialogViewModel.OnDismissRequested()
    {
        if (CancelButtonIndex >= 0)
            OnButtonClick(CancelButtonIndex);
    }
}
