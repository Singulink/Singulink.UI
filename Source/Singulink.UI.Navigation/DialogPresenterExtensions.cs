namespace Singulink.UI.Navigation;

/// <summary>
/// Provides extension methods for <see cref="IDialogPresenter"/> to show message dialogs.
/// </summary>
public static class DialogPresenterExtensions
{
    /// <inheritdoc cref="ShowMessageDialogAsync(IDialogPresenter, string, string)"/>"
    public static async Task ShowMessageDialogAsync(this IDialogPresenter navigator, string message)
    {
        var options = new MessageDialogOptions(message, DialogButtonLabels.OK);
        await ShowMessageDialogAsync(navigator, options);
    }

    /// <summary>
    /// Shows a message dialog with an "OK" button and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="navigator">The instance of the navigator to use to show the dialog.</param>
    /// <param name="message">The message for the dialog.</param>
    /// <param name="title">The title for the dialog.</param>
    public static async Task ShowMessageDialogAsync(this IDialogPresenter navigator, string message, string title)
    {
        var options = new MessageDialogOptions(message, DialogButtonLabels.OK) { Title = title };
        await ShowMessageDialogAsync(navigator, options);
    }

    /// <inheritdoc cref="ShowMessageDialogAsync(IDialogPresenter, string, string, IEnumerable{string})"/>"
    public static async Task<int> ShowMessageDialogAsync(this IDialogPresenter navigator, string message, IEnumerable<string> buttonLabels)
    {
        var options = new MessageDialogOptions(message, buttonLabels);
        return await ShowMessageDialogAsync(navigator, options);
    }

    /// <summary>
    /// Shows a message dialog with the specified button labels and returns a task that completes with the button index that was clicked when the dialog closes.
    /// </summary>
    /// <param name="navigator">The instance of the navigator to use to show the dialog.</param>
    /// <param name="message">The message for the dialog.</param>
    /// <param name="title">The title for the dialog.</param>
    /// <param name="buttonLabels">A list of labels for the buttons displayed in the dialog. At least one label must be specified.</param>
    public static async Task<int> ShowMessageDialogAsync(this IDialogPresenter navigator, string message, string title, IEnumerable<string> buttonLabels)
    {
        var options = new MessageDialogOptions(message, buttonLabels) { Title = title };
        return await ShowMessageDialogAsync(navigator, options);
    }

    /// <summary>
    /// Shows a message dialog with the specified options and returns a task that completes with the button index that was clicked when the dialog closes. The
    /// options allow some additional customization of the dialog, such as configuring the default and cancel buttons.
    /// </summary>
    /// <param name="navigator">The instance of the navigator to use to show the dialog.</param>
    /// <param name="options">The options for the dialog, such as title, message and button labels.</param>
    public static async Task<int> ShowMessageDialogAsync(this IDialogPresenter navigator, MessageDialogOptions options)
    {
        var viewModel = new MessageDialogViewModel(options);
        await navigator.ShowDialogAsync(viewModel);
        return viewModel.ResultButtonIndex;
    }
}
