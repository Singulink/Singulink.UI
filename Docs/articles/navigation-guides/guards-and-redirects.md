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
- `OnRouteNavigatingAsync` fires for **any** navigation involving the current route — including sibling child swaps. Use it on parent view models to guard the whole subtree.

`NavigatingArgs.NavigationType` indicates whether the navigation is a `Normal`, `Back`, `Forward` or `Refresh` operation — use this to allow back/forward to bypass the guard if appropriate:

```csharp
if (args.NavigationType == NavigationType.Normal && HasUnsavedChanges)
{
    // only guard normal navigations
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
        args.Redirect = Redirect.NavigatePartial<RepoViewModel>(Routes.Repo.HomePage);
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

When the user closes the window, the navigator intercepts the close event, runs `TryShutDownAsync()` (which calls guards on each active view model), and only actually closes the window if all guards allow. This provides unsaved-changes prompts on window close without any extra code in your view models.

</div>
