<div class="article">

# WinUI / Uno Setup

This guide covers the host-side configuration needed to wire a `Navigator` into a WinUI or Uno Platform application.

## Installing the Packages

Add the `Singulink.UI.Navigation.WinUI` package to your client project. It brings in `Singulink.UI.Navigation` transitively, so view-model projects only need the core package.

```xml
<!-- In your client project (WinUI / Uno app): -->
<PackageReference Include="Singulink.UI.Navigation.WinUI" Version="..." />

<!-- In your view model project: -->
<PackageReference Include="Singulink.UI.Navigation" Version="..." />
```

## Creating the Navigator

The typical place to create the navigator is in the constructor of your main window (or any other window that hosts navigable content). The simplest setup passes the window directly to the navigator, which installs a control that will host navigated views:

```csharp
using Microsoft.UI.Xaml;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.WinUI;

public sealed partial class MainWindow : Window
{
    public Navigator Navigator { get; }

    public MainWindow()
    {
        Navigator = new Navigator(this, ConfigureNavigator);
        Navigator.HookWindowActivatedEvent(this, InitialNavigateAsync, OnInitialNavigationFailedAsync);
        Navigator.HookSystemNavigationRequests();
        Navigator.HookWindowClosedEvents(this);
    }

    private static void ConfigureNavigator(NavigatorBuilder builder)
    {
        builder.Services = ((App)Application.Current).Services;

        // Routes
        builder.MapRoutedView<MainViewModel, MainPage>();
        builder.MapRoutedView<HomeViewModel, HomePage>();
        builder.MapRoutedView<RepoViewModel, RepoPage>();
        builder.MapRoutedView<DocumentViewModel, DocumentPage>();

        // Dialogs
        builder.MapDialog<ConfirmDeleteDialogViewModel, ConfirmDeleteDialog>();

        // Register route definitions (see Defining Routes guide)
        Routes.AddAllRoutes(builder);

        // Optional: tune stack sizes and caching
        builder.ConfigureNavigationStacks(maxSize: 30, maxBackCachedDepth: 10, maxForwardCachedDepth: 5);
    }

    private static Task InitialNavigateAsync(Navigator navigator)
    {
#if __WASM__
        return navigator.NavigateAsync(Navigator.GetBrowserRoute());
#else
        return navigator.NavigateAsync("/");
#endif
    }

    private static async Task OnInitialNavigationFailedAsync(Navigator navigator, NavigationRouteException ex)
    {
        await navigator.ShowMessageDialogAsync(ex.Message, "Navigation Error");
        await navigator.NavigateAsync("/");
    }
}
```

#### Navigator Constructor Overloads

There are three `Navigator` constructors:

- `Navigator(Window window, Action<NavigatorBuilder> buildAction)`: the recommended overload for typical apps. The navigator manages the window's content and uses it to host views.
- `Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)`: use this when you want custom XAML chrome around the navigator (e.g. a window with a navigation rail, status bar, or other surrounding UI where only part of the window hosts navigated views). Provide your own `ContentControl` placed wherever you want navigated content to appear.
- `Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)`: accepts a pre-built `ViewNavigator` for advanced scenarios where the host control is customized.

## Mapping Views and Dialogs

- `builder.MapRoutedView<TViewModel, TView>()`: maps a routed view model to a view (typically a `UserControl`). Parent view types must implement `IParentView` (see [Parent Views and Child Navigation](parent-views.md)).
- `builder.MapDialog<TDialogViewModel, TDialog>()`: maps a dialog view model to a `ContentDialog`. See [Dialogs](dialogs.md).

Both calls validate their arguments: if `TView` doesn't have a parameterless constructor, or if a view model used as a parent doesn't have an `IParentView` view type, the builder throws at startup (before the first navigation happens).

> [!IMPORTANT]
> Use `UserControl` (not `Page`) as the base type for views mapped via `MapRoutedView`. The framework hosts views inside a `ContentControl` (or another `ViewNavigator` host), not inside a `Frame`, so `Page`-specific navigation events (`OnNavigatedTo`, `OnNavigatedFrom`) never fire. `UserControl` is the recommended base type for routed views; the framework's own `IRoutedViewModel` lifecycle methods replace anything you'd otherwise hook on `Page`.

## Hooking System Events

Three optional hooks integrate the navigator with OS-level and window lifetime behaviors:

#### HookSystemNavigationRequests()

Subscribes to `SystemNavigationManager.BackRequested` on platforms that provide it (Uno mobile, WASM), translating system back gestures into `HandleSystemBackRequest` calls on the navigator. On Windows it's a no-op since Windows does not expose a system back button.

#### HookWindowClosedEvents(Window window)

Intercepts the window close event and runs `TryShutDownAsync()` before letting the window actually close. This gives active view models a chance to cancel (e.g. unsaved changes prompts). See [Guards and Redirects](guards-and-redirects.md) for more detail.

On WebAssembly this hook installs a browser `beforeunload` guard so that closing the tab, refreshing the page, or navigating to an external URL still consults active-route guards. Because `beforeunload` requires a synchronous decision, asynchronous guard implementations fall back to the browser's native "Leave Site?" prompt - see [Guards and Redirects: WebAssembly](guards-and-redirects.md#webassembly-browser-tab-close-refresh-and-external-navigation) for details.

#### HookWindowActivatedEvent(Window, initialNavigationAction, fallbackAction)

Defers initial navigation until the window is first activated, which is the correct point in the WinUI / Uno lifecycle to navigate (the XAML root is fully ready and dispatcher work runs reliably). The hook also ensures the initial navigation runs only once and routes any `NavigationRouteException` thrown by the initial action into the supplied fallback action. This is useful for showing an error message and falling back to a known-good route when a deep link can't be resolved.

Must be called **before** the window is activated. Trying to navigate from the window constructor or via `_ = SomeAsync()` fire-and-forget is unreliable; use this hook instead.

Most applications want all three hooks enabled in their main window.

#### Mouse Back / Forward Buttons

XButton1 / XButton2 (the thumb buttons on most mice) are handled automatically by the navigator; no configuration is needed. Pressing them dispatches to `HandleSystemBackRequest` / `HandleSystemForwardRequest` respectively.

## WebAssembly / Browser Deep Links

On WASM, browsers deep-link to specific routes by setting the URL before the app loads. Retrieve the initial route with the `Navigator.GetBrowserRoute()` static helper:

```csharp
#if __WASM__
    string route = Navigator.GetBrowserRoute();   // e.g. "/r/my-repo/document/42"
#endif
```

Then pass it to `NavigateAsync` as the application's initial navigation. The navigator keeps the browser URL synchronized with `CurrentRoute` automatically thereafter, so back / forward browser buttons, shareable URLs, and bookmarks all work out of the box.

## Tuning Caching

By default the navigator caches a limited number of back-stack and forward-stack views in memory (views and view models are recreated if the user navigates past that depth). The default values are exposed as constants on `INavigatorBuilder`: [`DefaultNavigationStacksSize`](../../api/Singulink.UI.Navigation.INavigatorBuilder.DefaultNavigationStacksSize.html), [`DefaultMaxBackStackCachedDepth`](../../api/Singulink.UI.Navigation.INavigatorBuilder.DefaultMaxBackStackCachedDepth.html), and [`DefaultMaxForwardStackCachedDepth`](../../api/Singulink.UI.Navigation.INavigatorBuilder.DefaultMaxForwardStackCachedDepth.html). They can be overridden via:

```csharp
builder.ConfigureNavigationStacks(
    maxSize: 30,               // max entries in each stack
    maxBackCachedDepth: 10,    // keep the 10 most-recent back entries materialized
    maxForwardCachedDepth: 5);
```

See `INavigatorBuilder.ConfigureNavigationStacks` in the API reference for full details. Cached entries retain their view and view model instances; entries beyond the cached depth release those references but stay in the stack (by URL), and will be re-materialized with fresh view model instances if navigated to again.

</div>
