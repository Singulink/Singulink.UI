<div class="article">

# Guards and Redirects

Routed view models can veto navigations away from them, or redirect navigations to them. This guide covers the guard and redirect APIs and shows common patterns like confirm-on-unsaved-changes and default-child redirects.

## Cancelling a Navigation

`OnNavigatingAwayAsync(NavigatingArgs args)` and `OnRouteNavigatingAsync(NavigatingArgs args)` are invoked before any navigation that would unmount the view model. Either can set `args.Cancel = true` to veto the navigation:

```csharp
public override async Task OnNavigatingAwayAsync(NavigatingArgs args)
{
    if (!HasUnsavedChanges)
        return;

    int result = await this.Navigator.ShowMessageDialogAsync(
        "You have unsaved changes. Discard them?",
        "Unsaved Changes",
        DialogButtonLabels.YesNo);

    if (result != 0)
        args.Cancel = true;
}
```

- `OnNavigatingAwayAsync` fires only when **this** view model is being unmounted (its route part is changing).
- `OnRouteNavigatingAsync` fires for **any** navigation involving the current route, including sibling child swaps. Use it on parent view models to guard the whole subtree.

`NavigatingArgs.NavigationType` indicates whether the navigation is a `Normal`, `Back`, `Forward` or `Refresh` operation. Use this to allow back/forward to bypass the guard if appropriate:

```csharp
if (args.NavigationType == NavigationType.Normal && HasUnsavedChanges)
{
    // only guard normal navigations
}
```

### WebAssembly: Browser Tab Close, Refresh, and External Navigation

In-app navigation (including the browser back / forward buttons) goes through the navigator's normal asynchronous pipeline, so `OnNavigatingAwayAsync` and `OnRouteNavigatingAsync` can freely `await` work such as confirmation dialogs.

Closing the tab, refreshing the page, or navigating to an external URL is different. The browser delivers these as a `beforeunload` event which requires a **synchronous** decision; the navigator cannot await your guard methods. The navigator still invokes the guards synchronously, but if any guard on a view model in the active route does not complete synchronously, the navigator must block the unload immediately and the browser shows its native prompt:

> Leave Site? Changes you made may not be saved.

The wording is browser-controlled and cannot be customized. If the user chooses to leave, the app is terminated immediately - any async work started by the guard is abandoned. If the user chooses to stay, the in-flight guard task continues running to completion in the background, so a subsequent close attempt may complete synchronously without prompting.

Key points:

- The fallback prompt is triggered if **any** `OnNavigatingAwayAsync` or `OnRouteNavigatingAsync` implementation on **any** active-route view model goes async (i.e. awaits an incomplete task), regardless of whether the view model would have set `args.Cancel`.
- A guard that completes synchronously - whether by returning `Task.CompletedTask`, by being an `async` method that never awaits an incomplete task, or by synchronously setting `args.Cancel = true` - is fully respected and produces no prompt unless `args.Cancel` is `true`.
- The guard is only installed when [`HookWindowClosedEvents(Window)`](winui-setup.md#hookwindowclosedeventswindow-window) has been called.

For view models that need to behave well across both paths, cache dirty state synchronously and set `args.Cancel` from a synchronous code path. Async confirmation dialogs can still be used; they will be honored on the in-app navigation path and gracefully degrade to the native prompt on tab close / refresh:

```csharp
public override async Task OnNavigatingAwayAsync(NavigatingArgs args)
{
    if (!HasUnsavedChanges)
        return;

    // On WASM tab-close / refresh, the browser's native "Leave Site?" prompt is
    // shown first because this method goes async. If the user chooses to leave,
    // the app is terminated immediately; otherwise, execution continues here and
    // the dialog below is shown.
    int result = await this.Navigator.ShowMessageDialogAsync(
        "You have unsaved changes. Discard them?",
        "Unsaved Changes",
        DialogButtonLabels.YesNo);

    if (result != 0)
        args.Cancel = true;
}
```

## Redirecting a Navigation

`OnNavigatedToAsync(NavigationArgs args)` and `OnRouteNavigatedAsync(NavigationArgs args)` run after the view model has been materialized but before the user can interact with it. Setting `args.Redirect` causes the navigator to immediately perform a different navigation.

```csharp
public override Task OnNavigatedToAsync(NavigationArgs args)
{
    if (!_session.IsAuthenticated)
    {
        args.Redirect = Redirect.Navigate(Routes.LoginRoot);
    }

    return Task.CompletedTask;
}
```

The `Redirect` class provides static factory methods that mirror the navigator's own APIs:

| Factory | Equivalent |
|---|---|
| `Redirect.Navigate(string)` | `NavigateAsync(string)` |
| `Redirect.Navigate(rootPart, ...)` | `NavigateAsync(rootPart, ...)` |
| `Redirect.NavigatePartial(...)` | `NavigatePartialAsync(...)` |
| `Redirect.NavigateToParent<T>()` | `NavigateToParentAsync<T>()` |
| `Redirect.GoBack()` | `GoBackAsync()` |

## Default Child Redirect Pattern

A parent view model often wants to redirect to a default child when the user navigates to the parent alone:

```csharp
public override Task OnRouteNavigatedAsync(NavigationArgs args)
{
    // Only redirect when the navigation stops at this parent (no child specified).
    if (!args.HasChildNavigation)
    {
        args.Redirect = Redirect.NavigatePartial(Routes.Repo.HomePage);
    }

    return Task.CompletedTask;
}
```

`args.HasChildNavigation` is `true` when the navigation target includes a descendant route beneath this view model; skipping the redirect in that case avoids overriding the user's intended destination.

## Graceful Shutdown

The guard methods are also consulted by `TryShutDownAsync()` (see [Navigating](navigating.md#graceful-shutdown)). The WinUI navigator provides `HookWindowClosedEvents(Window)` which ties everything together:

```csharp
var navigator = new Navigator(MainContent, ConfigureNavigator);
navigator.HookWindowClosedEvents(this);   // 'this' is the Window
```

When the user closes the window, the navigator intercepts the close event, runs `TryShutDownAsync()` (which calls guards on each active view model), and only actually closes the window if all guards allow. This provides unsaved-changes prompts on window close without any extra manaul wiring.

</div>
