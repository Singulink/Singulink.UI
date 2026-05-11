<div class="article">

# Dialogs

Dialogs are modal view models that surface temporary interactions like confirmations, forms, pickers, and alerts. The framework handles showing them, waiting for them to close, returning results, and integrating with system back / escape-key dismissal.

## Mapping a Dialog

Dialog view models are mapped to `ContentDialog` subclasses at navigator build time, similar to routed views:

```csharp
builder.MapDialog<ConfirmDeleteDialogViewModel, ConfirmDeleteDialog>();
builder.MapDialog<RegisterDialogViewModel, RegisterDialog>();
builder.MapDialog<SelectItemDialogViewModel, SelectItemDialog>();
```

The dialog view type must extend `ContentDialog` and have a public parameterless constructor. The view model type can be any class implementing <xref:Singulink.UI.Navigation.IDialogViewModel>.

## Dialog Interfaces

There are four dialog interfaces you can implement:

| Interface | Use case |
|---|---|
| **IDialogViewModel** | Dialogs that don't return a result. |
| **IDialogViewModel\<TResult\>** | Dialogs that produce a typed result (e.g. the picked item). |
| **IDismissibleDialogViewModel** | Dialogs that handle escape-key / system-back dismissal. |
| **IDismissibleDialogViewModel\<TResult\>** | Combination of the above two. |

All of them inherit <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> from the base interface. Override it to perform initialization when the dialog appears.

## Wiring Up Dialog Buttons

`ContentDialog` exposes three built-in buttons (**primary**, **secondary**, and **close**), but its default behavior is awkward to drive from a view model: clicking a button auto-closes the dialog, command `CanExecute` doesn't sync to the button's enabled state, and intercepting a click usually means hooking a code-behind event handler.

This library improves on that significantly so you can drive dialogs entirely with commands and almost never need event handlers. The same rules apply uniformly to all three buttons:

- **A button with only text and no command auto-closes the dialog when clicked.** This makes simple "OK" / "Cancel" buttons trivial to set up: just provide the button text and you're done.
- **A button with a command wired to it does not auto-close the dialog.** The command is invoked on click and it is the command's responsibility to close the dialog by calling <xref:Singulink.UI.Navigation.IDialogNavigator.Close> on `this.Navigator` from the view model. This applies to **all three buttons**, including the close button, so you can intercept any button click with a command (e.g. to confirm dismissal with "Discard changes?").
- **For primary and secondary buttons, command's `CanExecute` is automatically synchronized with the button's `IsEnabled` state.** No extra binding is required and no `IsPrimaryButtonEnabled` / `IsSecondaryButtonEnabled` setup is needed.
- **The close button cannot be disabled.** `ContentDialog` does not expose a `CloseButtonEnabled` property, so the button always appears enabled in the UI even when its command's `CanExecute` returns `false`. Setting `CanExecute = false` will still prevent the command from running on click, but for the cleanest user experience it's better not to provide a `CanExecute` implementation for close button commands. To prevent the user from closing the dialog at certain times, hide the close button by setting `CloseButtonText` to an empty string.
- **Existing event handlers still work.** If you wire a `Click` event handler in addition to a command, you can set `args.Cancel = true` in the handler to suppress the command invocation for that click.

### Escape Key and Dismiss Requests

The close button has an additional role: it is also the target of the **escape key** and acts as the default integration point for dismiss requests.

- **Pressing escape triggers a click on the close button.** This means escape follows the exact same rules as clicking the button: if a command is wired, the command runs; otherwise the dialog auto-closes (or raises a dismiss request, see below).
- **If the close button is hidden (`CloseButtonText` is empty), escape key presses are ignored.** There is no button to click, so nothing happens.
- **If the view model implements <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel> and the close button has no command wired, clicking the close button raises a dismiss request** (<xref:Singulink.UI.Navigation.IDismissibleDialogViewModel.OnDismissRequestedAsync>) instead of auto-closing the dialog. The view model decides whether to close the dialog or veto the dismissal (e.g. after prompting "Discard changes?").
- **System back requests always raise a dismiss request when the view model implements <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel>**, regardless of the close button's state or whether a command is wired to it. This is the only way to handle system back, and unlike escape it works even when the close button is hidden.

> [!TIP]
> The typical pattern for a cancellable dialog is to set `CloseButtonText` (e.g. to "Cancel") **and** implement <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel>, with no command wired to the close button. This way both close button clicks (including escape key) and system back requests funnel through the same <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel.OnDismissRequestedAsync> method, giving you a single place to handle all cancel / dismiss logic. If you need close button clicks and escape key presses to be handled differently than system back requests, you can wire a command to the close button **and** implement <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel>. The command handles the close button (and escape key), while <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel.OnDismissRequestedAsync> handles system back.

#### Example: A Confirmation Dialog

View model:

```csharp
public partial class ConfirmDeleteDialogViewModel(string itemName)
    : ObservableObject, IDialogViewModel<bool>
{
    public string ItemName => itemName;
    public bool Confirmed { get; private set; }
    bool IDialogViewModel<bool>.Result => Confirmed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial bool ConfirmationChecked { get; set; }

    [RelayCommand(CanExecute = nameof(ConfirmationChecked))]
    private void Delete()
    {
        Confirmed = true;
        this.Navigator.Close();
    }
}
```

XAML (note no event handlers, no `IsPrimaryButtonEnabled`, no manual `Hide()` calls):

```xml
<ContentDialog x:Class="MyApp.Dialogs.ConfirmDeleteDialog"
               xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:MyApp.ViewModels"
               Title="Delete Item"
               PrimaryButtonText="Delete"
               PrimaryButtonCommand="{x:Bind Model.DeleteCommand}"
               CloseButtonText="Cancel">
    <ContentDialog.Resources>
        <vm:ConfirmDeleteDialogViewModel x:Key="DesignVM" />
    </ContentDialog.Resources>

    <StackPanel Spacing="12">
        <TextBlock Text="{x:Bind Model.ItemName}" />
        <CheckBox Content="I understand this cannot be undone."
                  IsChecked="{x:Bind Model.ConfirmationChecked, Mode=TwoWay}" />
    </StackPanel>
</ContentDialog>
```

```csharp
public sealed partial class ConfirmDeleteDialog : ContentDialog
{
    public ConfirmDeleteDialogViewModel Model => (ConfirmDeleteDialogViewModel)DataContext;

    public ConfirmDeleteDialog() => InitializeComponent();
}
```

The primary button stays disabled until the checkbox is checked (because `DeleteCommand.CanExecute` is false), clicking it runs the command which sets the result and closes the dialog, and clicking "Cancel" closes the dialog with `Confirmed = false`. No code-behind event handlers are involved.

## Simple Dialog (no result)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

public partial class InfoDialogViewModel(string message)
    : ObservableObject, IDialogViewModel
{
    public string Message => message;
}
```

From a routed view model:

```csharp
await this.Navigator.ShowDialogAsync(new InfoDialogViewModel("Saved."));
```

`this.Navigator` on a dialog view model returns an <xref:Singulink.UI.Navigation.IDialogNavigator>, which exposes <xref:Singulink.UI.Navigation.IDialogNavigator.Close*>, <xref:Singulink.UI.Navigation.IDialogNavigator.TaskRunner>, and <xref:Singulink.UI.Navigation.IDialogPresenter.ShowDialogAsync*> for nesting additional dialogs (see below).

> [!CAUTION]
> Unlike routed view models (which the navigator constructs), **dialog view models are instantiated by your code** and only get wired up to a navigator immediately before <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> fires. This means `this.Navigator` and `this.TaskRunner` are **not available in the dialog view model's constructor**, and attempting to access them there will throw. Defer any work that needs them to <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> or to a command/method that runs after the dialog is shown.

## Dialog with a Result

Implement <xref:Singulink.UI.Navigation.IDialogViewModel`1> and provide the result in the <xref:Singulink.UI.Navigation.IDialogViewModel`1.Result> property:

```csharp
public partial class PickNumberDialogViewModel : ObservableObject, IDialogViewModel<int?>
{
    [ObservableProperty]
    public partial int? SelectedNumber { get; private set; }

    int? IDialogViewModel<int?>.Result => SelectedNumber;

    [RelayCommand]
    private void Pick(int number)
    {
        SelectedNumber = number;
        this.Navigator.Close();
    }
}
```

Callers receive the result directly from <xref:Singulink.UI.Navigation.IDialogPresenter.ShowDialogAsync*>:

```csharp
int? picked = await this.Navigator.ShowDialogAsync(new PickNumberDialogViewModel());

if (picked is int n)
    ApplyNumber(n);
```

## Dismissible Dialogs

By default, system back requests on a showing dialog are ignored, and escape key presses behave like a close button click (auto-closing the dialog if the close button is visible and has no command wired). To handle system back requests, and to centralize close-button and escape handling, implement <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel>:

```csharp
public partial class ConfirmActionDialogViewModel(string prompt)
    : ObservableObject, IDismissibleDialogViewModel<bool>
{
    public bool Confirmed { get; private set; }
    bool IDialogViewModel<bool>.Result => Confirmed;

    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        this.Navigator.Close();
    }

    Task IDismissibleDialogViewModel.OnDismissRequestedAsync()
    {
        this.Navigator.Close();
        return Task.CompletedTask;
    }
}
```

<xref:Singulink.UI.Navigation.IDismissibleDialogViewModel.OnDismissRequestedAsync> is raised by system back requests, and also by close-button clicks (including escape key presses that activate the close button) when no command is wired to the close button. See [Escape Key and Dismiss Requests](#escape-key-and-dismiss-requests) for the full set of rules. You can show a confirmation in this method (e.g. "Discard changes?") before actually closing the dialog.

> [!NOTE]
> If the close button is hidden (`CloseButtonText` is empty), escape key presses are ignored even when the view model is dismissible. Only system back requests will raise <xref:Singulink.UI.Navigation.IDismissibleDialogViewModel.OnDismissRequestedAsync> in that case.

## Nested Dialogs

A dialog can show another dialog from any method or command using the same <xref:Singulink.UI.Navigation.IDialogPresenter.ShowDialogAsync*> API. The parent dialog is temporarily hidden while the nested dialog is shown, and restored when the nested dialog closes:

```csharp
[RelayCommand]
private async Task CreateNewItemAsync()
{
    var editDialog = new EditItemDialogViewModel();
    var edited = await this.Navigator.ShowDialogAsync(editDialog);

    if (edited is not null)
    {
        Items.Add(edited);
    }
}
```

There is no limit on dialog nesting depth.

## Message Dialogs

For simple "OK" / "Yes / No / Cancel" style prompts there's no need to define a dedicated view model. The <xref:Singulink.UI.Navigation.DialogPresenterExtensions.ShowMessageDialogAsync*> extension methods on <xref:Singulink.UI.Navigation.IDialogPresenter> wrap a built-in <xref:Singulink.UI.Navigation.MessageDialogViewModel>:

```csharp
// Simple OK prompt
await this.Navigator.ShowMessageDialogAsync("File saved.");

// Titled OK prompt
await this.Navigator.ShowMessageDialogAsync("File saved.", "Success");

// Custom buttons; returns the index of the clicked button
int result = await this.Navigator.ShowMessageDialogAsync(
    "Discard unsaved changes?",
    "Unsaved Changes",
    DialogButtonLabels.YesNoCancel);

if (result is 0) { ... }            // Yes
else if (result is 1) { ... }       // No
else if (result is 2) { ... }       // Cancel
```

<xref:Singulink.UI.Navigation.DialogButtonLabels> provides common button-label combinations (`OK`, `YesNo`, `YesNoCancel`, `OKCancel`, ...). For full control use <xref:Singulink.UI.Navigation.MessageDialogOptions>:

```csharp
var options = new MessageDialogOptions(
    "An error occurred: " + ex.Message,
    buttonLabels: ["Retry", "Cancel"])
{
    Title = "Error",
    DefaultButtonIndex = 0,
    CancelButtonIndex = 1,
};

int choice = await this.Navigator.ShowMessageDialogAsync(options);
```

The default <xref:Singulink.UI.Navigation.MessageDialogViewModel> is mapped to a built-in `ContentDialog` automatically; no manual <xref:Singulink.UI.Navigation.WinUI.NavigatorBuilder.MapDialog``2> call is needed.

## Initialization on Show

Override <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> for initialization work that should happen when the dialog appears:

```csharp
public partial class LoadUsersDialogViewModel(IUserService userService)
    : ObservableObject, IDialogViewModel<User?>
{
    [ObservableProperty]
    public partial IReadOnlyList<User>? Users { get; private set; } 

    public async Task OnDialogShownAsync()
    {
        Users = await userService.GetAllAsync();
    }
}
```

Note <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> is not re-invoked when a nested dialog closes and the dialog is restored; it fires exactly once per <xref:Singulink.UI.Navigation.IDialogPresenter.ShowDialogAsync*> call.

## Dialog Restrictions

- Dialogs shown from <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatedToAsync*> or <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnRouteNavigatedAsync*> must be closed before the task completes **or** the navigation must have no child (<xref:Singulink.UI.Navigation.NavigationArgs.HasChildNavigation> is `false`).
- Dialogs cannot be shown from <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatedAwayAsync>. Refer to the documentation of other navigation methods for other restrictions.
- Nested dialogs must be shown using the top dialog's `Navigator`.

</div>
