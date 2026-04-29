<div class="article">

# Dependency Injection

View models get their dependencies through standard constructor injection. This guide shows how services are wired, how child view models can access services from their ancestors, and the presenter-interface pattern for inverting control with view-specific services.

## Registering Root Services

Assign any `IServiceProvider` to `builder.Services` when configuring the navigator. Services registered here are available to all routed and dialog view models via constructor injection:

```csharp
public App()
{
    var services = new ServiceCollection();

    services.AddSingleton<IUserService, UserService>();
    services.AddSingleton<IDocumentService, DocumentService>();
    services.AddTransient<ISaveManager, SaveManager>();

    Services = services.BuildServiceProvider();
}

public IServiceProvider Services { get; }
```

```csharp
void ConfigureNavigator(NavigatorBuilder builder)
{
    builder.Services = ((App)Application.Current).Services;

    builder.MapRoutedView<HomeViewModel, HomePage>();
    // ...
}
```

Any registered service can then be injected:

```csharp
public partial class HomeViewModel(IUserService userService, IDocumentService documentService)
    : ObservableObject, IRoutedViewModel
{
    // ...
}
```

## Child Services

Sometimes an ancestor view model needs to provide a service instance to its descendants — a loaded document, an edit session, an async pipeline. Use `this.SetChildService<T>(service)` in the ancestor's lifecycle method, then take `T` as a constructor parameter in the descendant:

```csharp
public partial class RepoViewModel(IRepoService repoService)
    : ObservableObject, IRoutedViewModel<string>
{
    public override async Task OnNavigatedToAsync(NavigationArgs args)
    {
        var repo = await repoService.LoadAsync(this.Parameter);
        this.SetChildService(repo);
    }
}

public partial class DocumentViewModel(Repo repo, IDocumentService docService)
    : ObservableObject, IRoutedViewModel<int>
{
    // 'repo' was provided by RepoViewModel via SetChildService<Repo>()
}
```

Child services are resolved **ancestor-first**: when a descendant view model is activated, the navigator walks up the route hierarchy looking for a matching `SetChildService<T>` registration before falling back to `builder.Services`. This means a child service registration shadows any root registration of the same type.

In addition to services registered via `SetChildService`, an ancestor view model that **directly implements an interface** also satisfies that interface as a dependency for its descendants. For example, if a parent view hosts a breadcrumb trail and its view model implements `IBreadcrumbConfig`, child view models can take `IBreadcrumbConfig` as a constructor parameter and the navigator will inject the ancestor view model itself — no `SetChildService` call required.

A parent view model can also act as a full container for its descendants by implementing `IServiceProvider`. When a child view model is being constructed and the navigator can't satisfy a dependency from `SetChildService` registrations or directly-implemented interfaces, it walks up the route hierarchy and calls `GetService(Type)` on any ancestor view model that implements `IServiceProvider`. This lets a parent integrate an arbitrary DI container (e.g. a per-document scope) into the resolution chain before falling back to `builder.Services`.

Guidelines:

- Call `SetChildService` from `OnNavigatedToAsync` or `OnRouteNavigatedAsync` **before** any child activation. By the time a child view model is being constructed, the ancestor's `OnNavigatedToAsync` has already completed.
- Child services are scoped to the lifetime of the ancestor view model — when the ancestor is unmounted, child services registered on it become unavailable.
- Register each type at most once per view model. Calling `SetChildService` twice with the same type replaces the previous registration.

## Presenter Interface Pattern

View models often need to drive UI behavior that cannot be expressed declaratively in XAML — focusing a text box, scrolling to a specific item, flashing an animation. Define a presenter interface in your view model project, register an implementation from the parent view as a child service, then inject it into descendant view models.

Step 1 — define the interface in the view model project:

```csharp
public interface IDocumentPresenter
{
    void ScrollToLine(int lineNumber);
}
```

Step 2 — register an implementation from the parent view onto its parent view model:

```csharp
public sealed partial class RepoPage : UserControl, IParentView, IDocumentPresenter
{
    public RepoViewModel Model => (RepoViewModel)DataContext;

    public RepoPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Model.SetChildService<IDocumentPresenter>(this);
    }

    public ViewNavigator CreateChildViewNavigator() => ViewNavigator.Create(MainContent);

    public void ScrollToLine(int lineNumber) => Editor.ScrollToLine(lineNumber);
}
```

Step 3 — inject the presenter into descendant view models:

```csharp
public partial class DocumentViewModel(IDocumentPresenter presenter)
    : ObservableObject, IRoutedViewModel<int>
{
    [RelayCommand]
    private void JumpToError(CompileError error) => presenter.ScrollToLine(error.Line);
}
```

The descendant view model stays platform-agnostic and testable; the parent view supplies all XAML-specific concerns through a strongly-typed interface.

</div>
