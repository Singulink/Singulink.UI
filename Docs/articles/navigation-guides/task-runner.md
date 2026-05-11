<div class="article">

# TaskRunner

<xref:Singulink.UI.Tasks.TaskRunner> (from the [Singulink.UI.Tasks](https://github.com/Singulink/Singulink.UI) package) is a small but central piece of the navigation framework. Every navigator owns one, and every routed view model and dialog view model exposes it through `this.TaskRunner`. This guide explains what it does, how it integrates with navigation, and the patterns you'll use day-to-day.

### What It Does

<xref:Singulink.UI.Tasks.ITaskRunner> is a UI-thread-affine task dispatcher with three responsibilities:

1. **Tracking busy state.** Tasks run via <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAsync*> / <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*> increment a busy counter; while the counter is > 0, <xref:Singulink.UI.Tasks.TaskRunner.IsBusy> is `true`. The navigator wires this to the root content's `IsEnabled` so the entire UI is automatically disabled while busy work is in flight, with no per-control plumbing.
2. **Propagating exceptions to the UI thread.** When a fire-and-forget or background-thread call faults, the <xref:Singulink.UI.Tasks.TaskRunner> posts the exception to the UI thread so it crashes loudly and is debuggable, rather than ending up as a silent unobserved-task exception. (An `async void` method *started* on the UI thread already throws on the UI thread; the difference is that fire-and-forget on a <xref:Singulink.UI.Tasks.TaskRunner> is also tracked, so tests can observe completion via <xref:Singulink.UI.Tasks.TaskRunner.WaitForIdleAsync(System.Boolean)>.)
3. **Dispatching to the UI thread.** <xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)>, <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*>, and the <xref:Singulink.UI.Tasks.TaskRunner.HasThreadAccess> property let you marshal work between background threads and the UI thread cleanly.

<xref:Singulink.UI.Tasks.TaskRunner> requires a <xref:System.Threading.SynchronizationContext> at construction time, capturing the calling thread as its UI thread. Inside a navigator, this is set up for you automatically.

> [!NOTE]
> Only <xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)> and <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*> actually push their work to the UI thread. The other methods (<xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*>, <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*>, <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAsync*>, <xref:Singulink.UI.Tasks.TaskRunnerExtensions.EnterBusyScope(Singulink.UI.Tasks.ITaskRunner)>) run their async work on whatever thread invoked them. If you call them from a background thread, the async lambda runs on a background thread too. They only guarantee that *unhandled exceptions* are forwarded to the UI thread.

### How Navigation Uses It

Each navigator has its own <xref:Singulink.UI.Tasks.TaskRunner> instance and binds <xref:Singulink.UI.Tasks.TaskRunner.IsBusy> to the root content's `IsEnabled`:

- **All lifecycle methods are run as busy tasks.** <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatedToAsync*>, <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnRouteNavigatedAsync*>, <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatingAwayAsync*>, etc., are awaited under a busy scope. The UI is disabled for the duration, and child navigations don't begin until the returned task completes.
- **Navigation calls themselves are busy tasks.** <xref:Singulink.UI.Navigation.INavigator.NavigateAsync*>, <xref:Singulink.UI.Navigation.INavigator.GoBackAsync>, <xref:Singulink.UI.Navigation.INavigator.RefreshAsync>, etc., all run as busy tasks.
- **Dialog view models** get a separate <xref:Singulink.UI.Tasks.TaskRunner> whose busy state controls the dialog's `IsEnabled`, so the dialog disables itself during busy work without affecting the rest of the app.

This means you usually don't need to think about busy state during navigation; the framework handles it. You only reach for <xref:Singulink.UI.Tasks.TaskRunner> directly for *non-navigation* work like commands, background computation, and UI-thread marshalling.

## Common Patterns

#### Run a Busy Task from a [RelayCommand]

The most common use case: a command that performs async work which should disable the UI while it runs.

```csharp
[RelayCommand]
private async Task SaveAsync()
{
    await this.TaskRunner.RunAsBusyAsync(async () => {
        await _api.SaveAsync(Document);
        StatusMessage = "Saved.";
    });
}
```

While `SaveAsync` is in flight, <xref:Singulink.UI.Tasks.TaskRunner.IsBusy> is `true` and the root content is disabled, so buttons, text boxes, and child controls all become non-interactive automatically. Any exception thrown inside the lambda propagates normally to the awaiting caller.

> [!TIP]
> <xref:Singulink.UI.Tasks.TaskRunnerExtensions.EnterBusyScope(Singulink.UI.Tasks.ITaskRunner)> is a flexible alternative to the lambda-based methods. The busy state ends when the scope is disposed, so you can wrap any block of code (sync or async, partial or full method body) without restructuring it as a lambda. Just make certain that the scope is disposed (*always* wrap `EnterBusyScope()` in `using`):
>
> ```csharp
> [RelayCommand]
> private async Task SaveAsync()
> {
>     // Some non-busy preamble
>     if (!Validate())
>         return;
>
>     using (this.TaskRunner.EnterBusyScope())
>     {
>         await _api.SaveAsync(Document);
>         StatusMessage = "Saved.";
>     }
>
>     // More non-busy work
> }
> ```

#### Fire-and-Forget from a Synchronous Callback

When a non-async callback (an event handler, a converter, a `partial void OnXyzChanged` hook) needs to kick off async work, use <xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*> / <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*> instead of `async void`:

```csharp
partial void OnSelectedItemChanged(Item? value)
{
    if (value is null)
        return;

    this.TaskRunner.RunAsBusyAndForget(async () => {
        Details = await _api.LoadDetailsAsync(value.Id);
    });
}
```

Unlike `async void`, fire-and-forget calls on a <xref:Singulink.UI.Tasks.TaskRunner> are tracked, so tests can deterministically wait for them via <xref:Singulink.UI.Tasks.TaskRunner.WaitForIdleAsync(System.Boolean)>. Exceptions are also forwarded to the UI thread when the call originates from a background thread (an `async void` started on a background thread would terminate and silently drop the exception).

#### Break Out of the Busy Navigation Event

Lifecycle methods like <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatedToAsync*> are themselves busy tasks, and child view models won't begin activating until they complete. If you have background work that shouldn't block child activation or keep the UI disabled, kick it off with <xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*> (or <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*> if you want to unblock child activation but keep the UI disabled until it completes):

```csharp
public Task OnNavigatedToAsync(NavigationArgs args)
{
    Document = _cache.Get(DocumentId);

    // Continue prefetching in the background; OnNavigatedToAsync returns immediately
    // so child views can activate without waiting for the prefetch.
    this.TaskRunner.RunAndForget(async () => {
        RelatedItems = await _api.LoadRelatedAsync(DocumentId);
    });

    return Task.CompletedTask;
}
```

#### Update the UI from a Background Thread

A common pattern is starting work on a background thread (e.g. a long-running import, a streaming operation, a periodic poller) and posting progress updates back to the UI thread. <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*> and <xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)> are how you marshal those updates:

```csharp
[RelayCommand]
 private async Task ImportAsync()
{
    using (this.TaskRunner.EnterBusyScope())
    {
        await Task.Run(async () => {
            int total = await _importer.GetTotalCountAsync();

            // We're on a background thread here, so don't touch observable properties directly.
            this.TaskRunner.Post(() => Progress = 0);

            int processed = 0;

            await foreach (var record in _importer.StreamAsync())
            {
                await ProcessRecordAsync(record);
                processed++;

                int captured = processed;
                this.TaskRunner.Post(() => Progress = (double)captured / total);
            }

            await this.TaskRunner.SendAsync(() => StatusMessage = $"Imported {processed} records.");
        });
    }
}
```

- Use **<xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)>** for fire-and-forget UI updates (no awaiting, no return value).
- Use **<xref:Singulink.UI.Tasks.TaskRunner.SendAsync*>** when you need to await the UI-thread work or get a value back from it. It runs synchronously when already on the UI thread, so it's safe to call from anywhere.
- Check **<xref:Singulink.UI.Tasks.TaskRunner.HasThreadAccess>** if a method might be called from either thread and you want to skip the round-trip when already on the UI thread.

<xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)> and <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*> are the only <xref:Singulink.UI.Tasks.TaskRunner> methods that move work onto the UI thread. The others (<xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAsync*>, <xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*>, etc.) execute their lambda on whatever thread called them.

#### Bind Busy State to UI

The simplest way to react to busy state in XAML is to bind to the view's own `IsEnabled` property; the <xref:Singulink.UI.Tasks.TaskRunner> toggles it automatically as part of its busy-state integration with the navigator:

```xml
<ProgressRing IsActive="{x:Bind IsEnabled, Mode=OneWay}"
              Visibility="{x:Bind IsEnabled, Mode=OneWay}" />
```

No extra view model property or navigator binding is needed; a busy navigator disables the root content, the `IsEnabled` cascade reaches your view, and the binding picks up the change. Avoid exposing a `Navigator` property on the view model just for this purpose: that property would shadow the `this.Navigator` extension, forcing you to call the extension explicitly as a static method to obtain the navigator instance.

## Method Summary

| Method | Awaitable | Busy | Runs on thread | Use case |
|---|---|---|---|---|
| <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAsync*> | Yes | Yes | Caller's thread | Async command bodies, anywhere you'd normally `await` |
| <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*> | No | Yes | Caller's thread | Sync callbacks (event handlers, property-changed hooks) that need to start busy async work |
| <xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*> | No | No | Caller's thread | Background prefetch from inside a lifecycle method (to avoid blocking child activation) |
| <xref:Singulink.UI.Tasks.TaskRunnerExtensions.EnterBusyScope(Singulink.UI.Tasks.ITaskRunner)> | n/a | Yes | Caller's thread | Wrap a block of code in a busy scope without restructuring as a lambda |
| <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*> | Yes | No | **UI thread** | Marshal work onto the UI thread from a background thread (with awaiting / return value) |
| <xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)> | No | No | **UI thread** | Fire-and-forget marshal onto the UI thread |
| <xref:Singulink.UI.Tasks.TaskRunner.WaitForIdleAsync(System.Boolean)> | Yes | n/a | n/a | Tests: wait for outstanding work to drain |

Methods marked "Caller's thread" run their async lambda on whatever thread invoked them, and only forward unhandled exceptions to the UI thread. Use <xref:Singulink.UI.Tasks.TaskRunner.Post(System.Action)> / <xref:Singulink.UI.Tasks.TaskRunner.SendAsync*> (or wrap in <xref:System.Threading.Tasks.Task.Run*>) when you explicitly need work on a different thread.

</div>