<div class="article">

# Routed View Models and Lifecycle

### Overview

A routed view model is any class that implements `IRoutedViewModel` (no parameter) or `IRoutedViewModel<TParam>` (with a parameter). The navigator creates instances of these view models using constructor dependency injection when a matching route is navigated to.

## The Base Interface

`IRoutedViewModelBase` defines lifecycle hooks that all routed view models inherit. All methods have default implementations that do nothing, so you only implement the hooks you need:

```csharp
public interface IRoutedViewModelBase
{
    bool CanBeCached => true;

    Task OnNavigatedToAsync(NavigationArgs args);
    Task OnRouteNavigatedAsync(NavigationArgs args);
    Task OnNavigatingAwayAsync(NavigatingArgs args);
    Task OnRouteNavigatingAsync(NavigatingArgs args);
    Task OnNavigatedAwayAsync();
}
```

## Declaring a View Model

#### No parameters

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

public partial class HomePageViewModel(IUserService userService)
    : ObservableObject, IRoutedViewModel
{
    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        CurrentUser = await userService.GetCurrentUserAsync();
    }

    [ObservableProperty]
    public partial User? CurrentUser { get; private set; }
}
```

#### With a parameter

Implement `IRoutedViewModel<TParam>` and read the parameter via `this.Parameter`:

```csharp
public partial class DocumentPageViewModel(IDocumentService documents)
    : ObservableObject, IRoutedViewModel<long>
{
    public long DocumentId => this.Parameter;

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        Document = await documents.GetAsync(DocumentId);
    }

    [ObservableProperty]
    public Document? Document { get; private set; }
}
```

#### With a params model

```csharp
public partial class EditEntryViewModel(IEntryService entries)
    : ObservableObject, IRoutedViewModel<EditEntryParams>
{
    public long EntryId => this.Parameter.EntryId;
    public Guid? RevisionId => this.Parameter.RevisionId;

    public Task OnNavigatedToAsync(NavigationArgs args) { ... }
}
```

See [Defining Routes](defining-routes.md) for how parameter types are declared on the route side.

## Navigator and TaskRunner Accessors

Routed view models do not store the navigator directly; it is exposed through extension members:

```csharp
[RelayCommand]
private async Task ReloadAsync()
{
    await this.Navigator.ShowMessageDialogAsync("Reloading...");

    await this.TaskRunner.RunAsBusyAsync(async () => {
        await SomeLongRunningOperationAsync();
    });
}
```

`this.Navigator` returns the `INavigator` associated with the view model; `this.TaskRunner` returns its task runner. The extension properties can be accessed anytime, including in the view model's constructor, if needed.

The `TaskRunner` integrates with busy-state on the navigator so the UI automatically disables while long-running tasks are in flight. See the [TaskRunner guide](task-runner.md) for details and patterns.

> [!TIP]
> Lifecycle methods like `OnNavigatedToAsync`, `OnRouteNavigatedAsync`, etc., are themselves run as busy tasks on the `TaskRunner`, so the UI is already disabled for the duration of the returned task and child view models won't begin activating until it completes. As a result, `RunAsBusyAndForget` is rarely useful from inside a lifecycle method (the navigation event itself is already busy). Use `this.TaskRunner.RunAndForget(...)` from a lifecycle method when you want to "break out" of the busy navigation event and let work continue in the background without keeping the UI busy or blocking cascading child navigations:
>
> ```csharp
> public Task OnNavigatedToAsync(NavigationArgs args)
> {
>     this.TaskRunner.RunAndForget(async () => {
>         await PrefetchDataAsync();
>     });
>
>     return Task.CompletedTask;
> }
> ```


## Lifecycle Methods

The navigator drives view models through a well-defined set of lifecycle methods. Understanding when each fires is the key to writing correct navigation logic.

#### OnNavigatedToAsync(NavigationArgs args)

Called when the view model **first becomes active** in the current route. Use this hook to load initial state, subscribe to events, or perform one-time setup.

**Rules**:

- Called exactly once per activation. When the user navigates away and later comes back (and the view model is still cached), it is called again.
- Always paired with a future call to `OnNavigatedAwayAsync`.
- Can show dialogs, provided they are closed before the task completes **or** `args.HasChildNavigation` is `false` (see below).
- Can request a redirect by setting `args.Redirect` (see [Navigation Guards and Redirects](guards-and-redirects.md)).

#### OnRouteNavigatedAsync(NavigationArgs args)

Called **every time the current route changes while this view model remains active**. In particular:

- Fires after `OnNavigatedToAsync` on initial activation.
- Fires again each time a child route changes under a parent view model, or when the route is refreshed / its parameters update.

This is the right hook for parent view models that need to react to child route changes (e.g. update a breadcrumb or a highlighted menu item):

```csharp
public partial class MainViewModel : ObservableObject, IRoutedViewModel
{
    public Task OnRouteNavigatedAsync(NavigationArgs args)
    {
        // Update navigation UI based on the new route
        UpdateSelectedMenuItem();
        return Task.CompletedTask;
    }
}
```

Leaf view models (with no children) typically only use `OnNavigatedToAsync` since the two events always coincide for them.

#### OnNavigatingAwayAsync(NavigatingArgs args)

Called **before** the view model is navigated away from, allowing it to cancel the pending navigation. This is where you prompt the user about unsaved changes:

```csharp
public async Task OnNavigatingAwayAsync(NavigatingArgs args)
{
    if (!IsDirty)
        return;

    int choice = await this.Navigator.ShowMessageDialogAsync(
        "You have unsaved changes. Save before leaving?",
        "Unsaved Changes",
        DialogButtonLabels.YesNoCancel);

    if (choice is 0) // Yes
    {
        if (!await SaveAsync())
            args.Cancel = true;
    }
    else if (choice is 2) // Cancel
    {
        args.Cancel = true;
    }
}
```

Set `args.Cancel = true` to abort the navigation. The method is allowed to `await` asynchronous work including dialogs.

#### OnRouteNavigatingAsync(NavigatingArgs args)

Called when the current route is about to change **but this view model will remain active** in the new route (e.g. a parent view model whose children are being swapped). Rarely needed; use it only when you need to guard a route change that doesn't actually unmount the view model.

#### OnNavigatedAwayAsync()

Called when the view model is navigated away from (after any `OnNavigatingAwayAsync` has completed and the navigation was not cancelled). Use this hook to dispose resources, cancel outstanding work, and unhook events:

```csharp
public async Task OnNavigatedAwayAsync()
{
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = null;

    if (_hubConnection is not null)
        await _hubConnection.DisposeAsync();
}
```

This method cannot cancel or redirect the navigation. It is always paired with a previous call to `OnNavigatedToAsync`.

### Lifecycle Summary

For a simple leaf route:

```txt
OnNavigatedToAsync     (view model becomes active)
OnRouteNavigatedAsync  (fires together on initial activation)
...time passes, user triggers a new navigation...
OnNavigatingAwayAsync  (gives view model a chance to cancel)
OnNavigatedAwayAsync   (cleanup)
```

For a parent view model whose child route changes but the parent remains active:

```txt
[parent remains mounted]
OnRouteNavigatingAsync   (parent, if navigation needs to be guarded)
OnRouteNavigatedAsync    (parent, after child successfully swaps)
```

## Caching

By default, view model instances are cached when navigated away from so returning to them later is instant and preserves state. If a view model consumes significant memory or should always be recreated fresh, override `CanBeCached`:

```csharp
public partial class LargeReportViewModel : ObservableObject, IRoutedViewModel
{
    public bool CanBeCached => false;
}
```

When a view model with `CanBeCached = false` is navigated away from, it is disposed along with its view. Note that if a parent view model is evicted from cache and provided a service to a child, all of its children are evicted too. Cache depth limits are configured on the navigator builder (see [WinUI / Uno Setup](winui-setup.md)).

If a view model implements `IDisposable` or `IAsyncDisposable`, `Dispose`/`DisposeAsync` is called automatically when it is evicted.

## Default Child Redirects

When a parent view model loads but the URL did not specify a child segment, you typically want to redirect to a default child. Use `args.HasChildNavigation` inside `OnNavigatedToAsync`:

```csharp
public async Task OnNavigatedToAsync(NavigationArgs args)
{
    await LoadRepositoryAsync();

    if (!args.HasChildNavigation)
        args.Redirect = Redirect.NavigatePartial(Routes.Repo.HomePage);
}
```

See [Navigation Guards and Redirects](guards-and-redirects.md) for more on redirects.

</div>
