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

The dialog view type must extend `ContentDialog` and have a public parameterless constructor. The view model type can be any class implementing `IDialogViewModel`.

## Dialog Interfaces

There are four dialog interfaces you can implement:

| Interface | Use case |
|---|---|
| **IDialogViewModel** | Dialogs that don't return a result. |
| **IDialogViewModel\<TResult\>** | Dialogs that produce a typed result (e.g. the picked item). |
| **IDismissibleDialogViewModel** | Dialogs that handle escape-key / system-back dismissal. |
| **IDismissibleDialogViewModel\<TResult\>** | Combination of the above two. |

All of them inherit `OnDialogShownAsync()` from the base interface. Override it to perform initialization when the dialog appears.

## Wiring Up Dialog Buttons

`ContentDialog` exposes three built-in buttons (**primary**, **secondary**, and **close**), but its default behavior is awkward to drive from a view model: clicking a button auto-closes the dialog, command `CanExecute` doesn't sync to the button's enabled state, and intercepting a click usually means hooking a code-behind event handler.

This library improves on that significantly so you can drive dialogs entirely with commands and almost never need event handlers:

- **Primary and secondary buttons should be wired to commands.** The command's `CanExecute` is automatically synchronized with the button's `IsEnabled` state, with no extra binding required and no `IsPrimaryButtonEnabled` / `IsSecondaryButtonEnabled` setup needed.
- **Primary and secondary buttons do not auto-close the dialog.** Clicking them invokes the command; closing the dialog is the command's responsibility (call `this.Navigator.Close()` from the view model).
- **The close button auto-closes the dialog when clicked**, as long as you simply provide a `CloseButtonText` value. You don't need to wire a command for the typical "X / Cancel" behavior.
- **You can wire a command to the close button to intercept the click.** When a command is set, the dialog will not auto-close. The command must call `this.Navigator.Close()`. This is useful for confirming dismissal (e.g. "Discard changes?").
- **The close button cannot be disabled.** `ContentDialog` does not expose a `CloseButtonEnabled` property, so the button always appears enabled in the UI even when its command's `CanExecute` returns `false`. Setting `CanExecute = false` will still prevent the command from running on click, but for the cleanest user experience it's better not to provide a `CanExecute` implementation for close button commands. To prevent the user from closing the dialog at certain times, hide the close button by setting `CloseButtonText` to an empty string.
- **Existing event handlers still work.** If you wire a `Click` event handler in addition to a command, you can set `args.Cancel = true` in the handler to suppress the command invocation for that click.

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

`this.Navigator` on a dialog view model returns an `IDialogNavigator`, which exposes `Close()`, a `TaskRunner`, and `ShowDialogAsync` for nesting additional dialogs (see below).

> [!CAUTION]
> Unlike routed view models (which the navigator constructs), **dialog view models are instantiated by your code** and only get wired up to a navigator immediately before `OnDialogShownAsync` fires. This means `this.Navigator` and `this.TaskRunner` are **not available in the dialog view model's constructor**, and attempting to access them there will throw. Defer any work that needs them to `OnDialogShownAsync` or to a command/method that runs after the dialog is shown.

## Dialog with a Result

Implement `IDialogViewModel<TResult>` and expose a `Result` property:

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

Callers receive the result directly from `ShowDialogAsync`:

```csharp
int? picked = await this.Navigator.ShowDialogAsync(new PickNumberDialogViewModel());

if (picked is int n)
    ApplyNumber(n);
```

## Dismissible Dialogs

By default, pressing Escape or triggering a system-back request on a showing dialog is ignored. To opt into dismissal, implement `IDismissibleDialogViewModel`:

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

`OnDismissRequestedAsync` is the single hook for both Escape-key and system-back dismissal. You can show a confirmation in this method (e.g. "Discard changes?") before actually closing the dialog.

## Nested Dialogs

A dialog can show another dialog from any method or command using the same `ShowDialogAsync` API. The parent dialog is temporarily hidden while the nested dialog is shown, and restored when the nested dialog closes:

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

There is no limit on dialog nesting depth. System back or Escape always applies to the top-most dialog.

## Message Dialogs

For simple "OK" / "Yes / No / Cancel" style prompts there's no need to define a dedicated view model. The `ShowMessageDialogAsync` extension methods on `IDialogPresenter` wrap a built-in `MessageDialogViewModel`:

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

`DialogButtonLabels` provides common button-label combinations (`OK`, `YesNo`, `YesNoCancel`, `OKCancel`, ...). For full control use `MessageDialogOptions`:

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

The default `MessageDialogViewModel` is mapped to a built-in `ContentDialog` automatically; no manual `MapDialog` call is needed.

## Initialization on Show

Override `OnDialogShownAsync` for initialization work that should happen when the dialog appears:

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

Note `OnDialogShownAsync` is not re-invoked when a nested dialog closes and the dialog is restored; it fires exactly once per `ShowDialogAsync` call.

## Dialog Restrictions

- Dialogs shown from `OnNavigatedToAsync` or `OnRouteNavigatedAsync` must be closed before the task completes **or** the navigation must have no child (`args.HasChildNavigation` is `false`).
- Dialogs cannot be shown from `OnNavigatedAwayAsync()`. Refer to the documentation of other navigation methods for other restrictions.
- Nested dialogs must be shown using the top dialog's `Navigator`.

</div>
