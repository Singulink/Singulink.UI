namespace Singulink.UI.Navigation;

/// <summary>
/// Provides extension methods for <see cref="IDialogNavigatorBase"/> to show message dialogs.
/// </summary>
public static class DialogNavigatorExtensions
{
    /// <inheritdoc cref="ShowMessageDialogAsync(IDialogNavigatorBase, string, string)"/>"
    public static Task ShowMessageDialogAsync(this IDialogNavigatorBase navigator, string message)
    {
        return ShowMessageDialogAsync(navigator, message, string.Empty, DialogButtonLabels.OK);
    }

    /// <summary>
    /// Shows a message dialog with an "OK" button and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="navigator">The instance of the navigator to use to show the dialog.</param>
    /// <param name="message">The message for the dialog.</param>
    /// <param name="title">The title for the dialog.</param>
    public static Task<int> ShowMessageDialogAsync(this IDialogNavigatorBase navigator, string message, string title)
    {
        return ShowMessageDialogAsync(navigator, message, title, DialogButtonLabels.OK);
    }

    /// <inheritdoc cref="ShowMessageDialogAsync(IDialogNavigatorBase, string, string, IEnumerable{string})"/>"
    public static Task<int> ShowMessageDialogAsync(this IDialogNavigatorBase navigator, string message, IEnumerable<string> buttonLabels)
    {
        return ShowMessageDialogAsync(navigator, message, string.Empty, buttonLabels);
    }

    /// <summary>
    /// Shows a message dialog with the specified button labels and returns a task that completes with the button index that was clicked when the dialog closes.
    /// </summary>
    /// <param name="navigator">The instance of the navigator to use to show the dialog.</param>
    /// <param name="message">The message for the dialog.</param>
    /// <param name="title">The title for the dialog.</param>
    /// <param name="buttonLabels">A list of labels for the buttons displayed in the dialog. At least one label must be specified.</param>
    public static async Task<int> ShowMessageDialogAsync(this IDialogNavigatorBase navigator, string message, string title, IEnumerable<string> buttonLabels)
    {
        await navigator.ShowDialogAsync(navigator => new MessageDialogViewModel(navigator, message, title, buttonLabels), out var viewModel);
        return viewModel.ResultButtonIndex;
    }
}
