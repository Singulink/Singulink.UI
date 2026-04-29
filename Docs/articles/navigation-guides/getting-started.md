<div class="article">

# Getting Started

**Singulink.UI.Navigation** is a navigation framework for MVVM applications with an emphasis on maintainability, compile-time safety, testability, and full deep-linking support. It is designed for applications with hierarchical, route-based navigation where URLs map to a tree of view models and views.

## Packages

The framework is split into two NuGet packages:

- **Singulink.UI.Navigation** — The UI framework-agnostic core. Reference this from your view model project.
- **Singulink.UI.Navigation.WinUI** — The WinUI/Uno implementation. Reference this from your client (UI) project.

This separation means view models never directly depend on WinUI types, which keeps view models fully testable without any UI framework dependencies.

## Recommended Project Layout

A typical solution has at least two projects:

```
MyApp.ViewModels      <-- references Singulink.UI.Navigation
MyApp.Client          <-- references Singulink.UI.Navigation.WinUI (and MyApp.ViewModels)
```

The view model project defines routes and view models. The client project defines views, maps views to view models and constructs the navigator.

## Minimal Example

### 1. Define a view model

In `MyApp.ViewModels`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace MyApp.ViewModels;

public partial class HomeViewModel : ObservableObject, IRoutedViewModel
{
    public Task OnNavigatedToAsync(NavigationArgs args)
    {
        // Load data, set up state, etc.
        return Task.CompletedTask;
    }
}
```

### 2. Define the routes

In `MyApp.ViewModels/Routes.cs`:

```csharp
using System.ComponentModel;
using Singulink.UI.Navigation;

namespace MyApp.ViewModels;

[Bindable(true)]
public static class Routes
{
    public static RootRoutePart<HomeViewModel> HomeRoot { get; } =
        Route.Build("/").Root<HomeViewModel>();

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRoute(HomeRoot);
    }
}
```

### 3. Create the view

In `MyApp.Client/Views/HomePage.xaml.cs`:

```csharp
using Microsoft.UI.Xaml.Controls;
using MyApp.ViewModels;

namespace MyApp.Client.Views;

public sealed partial class HomePage : UserControl
{
    public HomeViewModel Model => (HomeViewModel)DataContext;

    public HomePage() => InitializeComponent();
}
```

### 4. Configure the navigator and start navigating

In the main application window:

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyApp.Client.Views;
using MyApp.ViewModels;
using Singulink.UI.Navigation.WinUI;

namespace MyApp.Client;

public class AppWindow : Window
{
    private readonly Navigator _navigator;

    public AppWindow()
    {
        var rootContent = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        Content = rootContent;

        _navigator = new Navigator(rootContent, builder => {
            builder.MapRoutedView<HomeViewModel, HomePage>();
            builder.AddAllRoutes();
        });

        _navigator.HookWindowActivatedEvent(this, n => n.NavigateAsync(Routes.HomeRoot));
        _navigator.HookSystemNavigationRequests();
        _navigator.HookWindowClosedEvents(this);
    }
}
```

That's the complete loop: routes define structure, view models define behavior, views render state, and the navigator ties them together.

## Next Steps

Learn more about each area of the framework:

- [Defining Routes](defining-routes.md) — Route hierarchy, parameters, query strings, and the `Routes` class pattern.
- [Routed View Models and Lifecycle](view-models.md) — `IRoutedViewModel`, parameters, and lifecycle methods.
- [Navigating](navigating.md) — Navigating between routes, history, and system back integration.
- [Parent Views and Child Navigation](parent-views.md) — Hosting child views inside parent views.
- [Dialogs](dialogs.md) — Showing dialogs and returning results.
- [Navigation Guards and Redirects](guards-and-redirects.md) — Canceling and redirecting navigations.
- [Dependency Injection and Shared State](dependency-injection.md) — Wiring up services and sharing state between view models.
- [TaskRunner](task-runner.md) — Managing busy state, fire-and-forget tasks, and UI-thread dispatch.
- [WinUI / Uno Setup](winui-setup.md) — Platform-specific setup, deep linking, and browser URL integration.

You can also check out the [Playground](https://github.com/Singulink/Singulink.UI/tree/main/Playground) project for a runnable sample that exercises most of the framework.

</div>
