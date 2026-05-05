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

The typical place to create the navigator is in the constructor of your main window (or any other window that hosts navigable content). Provide a root `ContentControl` (or a custom `ViewNavigator`) and a configuration action:

```csharp
using Microsoft.UI.Xaml;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.WinUI;

public sealed partial class MainWindow : Window
{
    public Navigator Navigator { get; }

    public MainWindow()
    {
        InitializeComponent();

        Navigator = new Navigator(RootContent, ConfigureNavigator);
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

```xml
<!-- MainWindow.xaml -->
<Window ... >
    <ContentControl x:Name="RootContent"
                    HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Stretch" />
</Window>
```

#### Navigator Constructor Overloads

There are two `Navigator` constructors:

- `Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)`: convenience overload that creates a `ViewNavigator` for the given content control.
- `Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)`: accepts a pre-built `ViewNavigator` for custom scenarios (e.g. using a non-`ContentControl` host).

Use `ViewNavigator.Create(...)` to build a `ViewNavigator` around various XAML controls supported by the framework.

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

By default the navigator keeps 5 levels of back-stack and 5 levels of forward-stack views cached in memory (views and view models are recreated if the user navigates past that depth). For larger apps or apps with expensive view model construction, tune this via:

```csharp
builder.ConfigureNavigationStacks(
    maxSize: 30,               // max entries in each stack
    maxBackCachedDepth: 10,    // keep the 10 most-recent back entries materialized
    maxForwardCachedDepth: 5);
```

See `INavigatorBuilder.ConfigureNavigationStacks` in the API reference for full details. Cached entries retain their view and view model instances; entries beyond the cached depth release those references but stay in the stack (by URL), and will be re-materialized with fresh view model instances if navigated to again.

</div>
