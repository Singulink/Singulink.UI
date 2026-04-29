<div class="article">

# Navigating

The navigator is accessed from routed view models through `this.Navigator` and from the host (window or top-level control) directly as the `Navigator` instance. This guide covers the navigation APIs you'll use day-to-day.

## Navigating to a Concrete Route

Parameter-less root routes can be navigated to directly:

```csharp
await this.Navigator.NavigateAsync(Routes.LoginRoot);
```

Parameterized routes require a concrete instance created via `ToConcrete`:

```csharp
await this.Navigator.NavigateAsync(Routes.RepoRoot.ToConcrete("my-repo"));
```

You can compose root + child routes in a single call — the overloads match up to four levels deep:

```csharp
await this.Navigator.NavigateAsync(
    Routes.RepoRoot.ToConcrete("my-repo"),
    Routes.Repo.DocumentPage.ToConcrete(new DocumentParams { DocumentId = 42 }));
```

All `NavigateAsync` overloads accept an optional `anchor` argument for URL fragments:

```csharp
await this.Navigator.NavigateAsync(Routes.HomeRoot, anchor: "about");
```

## Navigating from a URL String

The navigator also accepts raw URL strings. This is how deep links from browsers, command-line arguments and saved links are handled:

```csharp
await _navigator.NavigateAsync("/r/my-repo/document/42");
```

If the URL is malformed or doesn't match any registered route, a `NavigationRouteException` is thrown. Use try/catch around initial-navigation calls to fall back to a safe route:

```csharp
try
{
    await _navigator.NavigateAsync(initialRoute);
}
catch (NavigationRouteException ex)
{
    await _navigator.ShowMessageDialogAsync(
        $"Could not navigate to '{initialRoute}': {ex.Message}", "Navigation Error");
    await _navigator.NavigateAsync("/");
}
```

## Partial Navigation

`NavigatePartialAsync` swaps child routes without re-navigating the parent. This is the preferred way to switch between pages that share the same parent:

```csharp
[RelayCommand]
private async Task ShowHomeAsync()
{
    await this.Navigator.NavigatePartialAsync(Routes.Repo.HomePage);
}
```

The generic parameters describe the parent view model the child is registered under; the navigator verifies at runtime that the current route actually contains that parent. If it doesn't, an `InvalidOperationException` is thrown.

A parameter-less overload `NavigatePartialAsync(string? anchor)` updates only the anchor on the current route. This fires the usual `OnRouteNavigating` / `OnRouteNavigated` lifecycle events, so view models that react to route changes (e.g. to update a highlighted item or scroll position) will see the new anchor. If you only want to reflect an anchor change in the URL without firing any lifecycle events, use [`UpdateCurrentRoute(anchor)`](#anchor-only-update) instead — the two methods are otherwise equivalent.

## Back, Forward, Refresh

```csharp
await this.Navigator.GoBackAsync();
await this.Navigator.GoForwardAsync();
await this.Navigator.RefreshAsync();
```

Each of these returns a `NavigationResult` (see below). Corresponding properties support binding their "can execute" state to UI:

- `CanGoBack`
- `CanGoForward`
- `CanRefresh`

```xml
<Button Content="Back"
        Command="{x:Bind Model.GoBackCommand}"
        IsEnabled="{x:Bind Model.Navigator.CanGoBack, Mode=OneWay}" />
```

To check whether there is any back / forward history regardless of current navigation state:

- `HasBackHistory`
- `HasForwardHistory`

## Navigation Results

Navigation methods return a `NavigationResult`:

- `NavigationResult.Success` — The navigation completed successfully.
- `NavigationResult.Cancelled` — The navigation was cancelled (e.g. by a view model's `OnNavigatingAwayAsync` setting `args.Cancel = true`).

Callers rarely need to inspect this directly; it's useful when chaining navigations or when the caller needs to know whether a guard prevented a navigation.

## The Current Route

`INavigator.CurrentRoute` returns a `NavigatorRoute` describing the current navigation state:

```csharp
NavigatorRoute current = this.Navigator.CurrentRoute;

string path = current.Path;                                  // e.g. "r/my-repo/home"
IReadOnlyList<IConcreteRoutePart> parts = current.Parts;
string? anchor = current.Anchor;

string full = current.ToString();                            // path + query + anchor
```

`ToString()` is particularly useful for building shareable URLs:

```csharp
string shareUrl = $"{Hosts.AppBaseUrl}/{this.Navigator.CurrentRoute}";
```

## Ancestor-Aware Checks

Use these methods to branch logic based on which view models are currently in the route tree:

```csharp
if (this.Navigator.CurrentRouteHasParent<RepoViewModel>())
{
    // currently inside a repository
}
```

```csharp
bool inRepoHome = this.Navigator.CurrentPathStartsWith(
    Routes.RepoRoot.ToConcrete("my-repo"),
    Routes.Repo.HomePage);
```

`CurrentPathStartsWith` only checks path equivalence; it does not require the current VM or view instances to match — useful for highlighting navigation items regardless of how the route was reached.

`GetCurrentRoutePartsToParent(Type parentViewModelType)` enumerates route parts up to a specific ancestor, which is handy when constructing breadcrumbs:

```csharp
foreach (var part in this.Navigator.GetCurrentRoutePartsToParent(typeof(MainViewModel)))
{
    // ...
}
```

## Navigation History

```csharp
IReadOnlyList<NavigatorRoute> backStack = this.Navigator.GetBackStack();
IReadOnlyList<NavigatorRoute> forwardStack = this.Navigator.GetForwardStack();
await this.Navigator.ClearHistoryAsync();
```

The returned stacks **do not include the current route** and are ordered most-recent-first. Stack sizes and caching depth are configured on the navigator builder — see [WinUI / Uno Setup](winui-setup.md).

## Updating the Current Route In-Place

Sometimes you need to reflect a change in URL state without performing a navigation. Two overloads support this:

### Anchor-only update

```csharp
this.Navigator.UpdateCurrentRoute(anchor: "section-2");
```

Useful for reacting to UI state like the currently-selected item in a scrollable list. Unlike [`NavigatePartialAsync(anchor)`](#partial-navigation), this method does **not** fire any `OnRouteNavigating` / `OnRouteNavigated` lifecycle events — it simply updates the URL in place. That's the only difference between the two; choose `UpdateCurrentRoute` when the anchor change is purely cosmetic and shouldn't be observed by view models, and `NavigatePartialAsync` when it should.

### Replacing the leaf route part

```csharp
// After the server assigns an ID to a newly-created entry, update the URL from
// /entries/new to /entries/{id} without re-navigating the view model.
this.Navigator.UpdateCurrentRoute(
    Routes.Repo.EntryPage.ToConcrete(newEntryId));
```

The new leaf route part must map to the same view model type as the current leaf, otherwise an `ArgumentException` is thrown. No lifecycle methods fire — the view model and view remain mounted while the URL updates.

## System Back / Forward Handling

On platforms where the OS or browser provides back / forward gestures (Android, iOS, WASM, some desktops), hook them via the WinUI navigator's `HookSystemNavigationRequests` method — see [WinUI / Uno Setup](winui-setup.md). Under the hood these dispatch to:

```csharp
bool handled = _navigator.HandleSystemBackRequest();
bool handled = _navigator.HandleSystemForwardRequest();
```

A back request returns `true` if any of the following happened: a dialog was dismissed, a light-dismiss popup was closed, a navigation is in progress, or a back navigation was initiated. This mirrors the convention expected by the OS — returning `false` allows the OS to take its default action (e.g. closing the app).

## Graceful Shutdown

`TryShutDownAsync()` attempts to close down the navigator gracefully by asking each active view model if it is ready to unload:

```csharp
if (await _navigator.TryShutDownAsync())
{
    // safe to close the window
}
else
{
    // a view model requested cancellation (e.g. unsaved changes prompt)
}
```

See [Navigation Guards and Redirects](guards-and-redirects.md) for the `HookWindowClosedEvents` convenience that wires this up automatically on window close.

</div>
