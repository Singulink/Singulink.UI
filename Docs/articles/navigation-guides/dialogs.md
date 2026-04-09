<div class="article">

# Dialogs

Dialogs are modal view models that surface temporary interactions — confirmations, forms, pickers, alerts. The framework handles showing them, waiting for them to close, returning results, and integrating with system back / escape-key dismissal.

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
| `IDialogViewModel` | Dialogs that don't return a result. |
| `IDialogViewModel<TResult>` | Dialogs that produce a typed result (e.g. the picked item). |
| `IDismissibleDialogViewModel` | Dialogs that handle escape-key / system-back dismissal. |
| `IDismissibleDialogViewModel<TResult>` | Combination of the above two. |

All of them inherit `OnDialogShownAsync()` from the base interface — override it to perform initialization when the dialog appears.

## Simple Dialog (no result)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

public partial class InfoDialogViewModel(string message)
    : ObservableObject, IDialogViewModel
{
    public string Message => message;

    [RelayCommand]
    private void Close() => this.Navigator.Close();
}
```

From a routed view model:

```csharp
await this.Navigator.ShowDialogAsync(new InfoDialogViewModel("Saved."));
```

`this.Navigator` on a dialog view model returns an `IDialogNavigator` — it exposes `Close()`, a `TaskRunner`, and `ShowDialogAsync` for nesting additional dialogs (see below).

## Dialog with a Result

Implement `IDialogViewModel<TResult>` and expose a `Result` property:

```csharp
public partial class PickNumberDialogViewModel : ObservableObject, IDialogViewModel<int?>
{
    [ObservableProperty]
    private int? _selectedNumber;

    int? IDialogViewModel<int?>.Result => SelectedNumber;

    [RelayCommand]
    private void Pick(int number)
    {
        SelectedNumber = number;
        this.Navigator.Close();
    }

    [RelayCommand]
    private void Cancel() => this.Navigator.Close();
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

    [RelayCommand]
    private void Cancel() => this.Navigator.Close();

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

The default `MessageDialogViewModel` is mapped to a built-in `ContentDialog` automatically — no manual `MapDialog` call is needed.

## Initialization on Show

Override `OnDialogShownAsync` for initialization work that should happen when the dialog appears:

```csharp
public partial class LoadUsersDialogViewModel(IUserService userService)
    : ObservableObject, IDialogViewModel<User?>
{
    [ObservableProperty]
    private IReadOnlyList<User>? _users;

    public async Task OnDialogShownAsync()
    {
        Users = await userService.GetAllAsync();
    }
}
```

Note `OnDialogShownAsync` is not re-invoked when a nested dialog closes and the dialog is restored — it fires exactly once per `ShowDialogAsync` call.

## Dialog Restrictions

- Dialogs shown from `OnNavigatedToAsync` or `OnRouteNavigatedAsync` must be closed before the task completes **or** the navigation must have no child (`args.HasChildNavigation` is `false`).
- Dialogs cannot be shown from `OnNavigatedAwayAsync()`. Refer to the documentation of other navigation methods for other restrictions.
- Nested dialogs must be shown using the top dialog's `Navigator`.

</div>
