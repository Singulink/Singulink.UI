<div class="article">

# Defining Routes

Routes describe the hierarchy of your application as a tree of strongly-typed route parts. Each route part maps to a view model, and together they form the URL structure of your app.

## Route Parts

There are two kinds of route parts:

- **Root route parts** — Top-level routes without a parent (e.g. `/login`, `/`, `/r/{repoId}`).
- **Child route parts** — Routes that appear under a specific parent view model (e.g. `home` under a repository root).

Root and child routes are built with the `Route.Build(...)` fluent API:

```csharp
using Singulink.UI.Navigation;

public static RootRoutePart<LoginViewModel> LoginRoot { get; } =
    Route.Build("/login").Root<LoginViewModel>();

public static ChildRoutePart<MainViewModel, HomeViewModel> HomeChild { get; } =
    Route.Build("home").Child<MainViewModel, HomeViewModel>();
```

The generic arguments make these strongly typed: a `ChildRoutePart<MainViewModel, HomeViewModel>` can only be registered under a parent route that maps to `MainViewModel`.

## View Model Parameters

A view model can accept a single **parameter** which is populated from the URL. The parameter type can be one of three things:

1. **A [params model](#params-models)** — A record annotated with `[RouteParamsModel]`, letting you pack multiple path and query values into one strongly-typed object.
2. **A single parsable type** — Any type that implements `IParsable<T>` and `IEquatable<T>` (e.g. `int`, `long`, `Guid`, `string`).
3. **`RouteQuery`** — If you only want a raw query string (e.g. arbitrary filter parameters), pass `RouteQuery` directly.

### Single Parsable Parameter

The simplest case — a single value parsed from the path:

```csharp
public static RootRoutePart<RepoViewModel, string> RepoRoot { get; } =
    Route.Build((string repoId) => $"r/{repoId}").Root<RepoViewModel>();

public static ChildRoutePart<RepoViewModel, DocumentViewModel, long> DocumentChild { get; } =
    Route.Build((long documentId) => $"document/{documentId}").Child<RepoViewModel, DocumentViewModel>();
```

The lambda describes the URL template using string interpolation — the parameters in the lambda are placeholders substituted into the path. At runtime, URL segments are parsed into the declared type.

The corresponding view model declares `IRoutedViewModel<T>`:

```csharp
public partial class DocumentViewModel : ObservableObject, IRoutedViewModel<long>
{
    public long DocumentId => this.Parameter;

    public Task OnNavigatedToAsync(NavigationArgs args) { ... }
}
```

### Params Models

When a route has multiple parameters or combines path and query values, use a **params model**:

```csharp
using Singulink.UI.Navigation;

[RouteParamsModel]
public partial record DocumentParams
{
    public required long DocumentId { get; init; }
    public required long VersionId { get; init; }
    public RouteQuery Query { get; init; }
}
```

The `[RouteParamsModel]` source generator implements `IRouteParamsModel<DocumentParams>` on the record, enabling bidirectional conversion between the record and a URL.

**Rules for params models**:

- Must be a `partial record`.
- All properties must be `init`-only.
- Non-nullable properties must be marked `required` (or be primary constructor parameters).
- Property types must be parsable (`IParsable<T>` + `IEquatable<T>`).
- Nullable properties correspond to values that may or may not be present. They can be populated from query string values (on [leaf view models](#query-string-and-leaf-view-models)) or from path holes in a [route group](#route-groups) where some patterns fill the hole and others omit it.
- At most one `RouteQuery` property is allowed. It is optional, can be named anything (`Query`, `Rest`, `Extras`, ...), and captures any query string values that don't match another property in the model.

#### Query String and Leaf View Models

Both required and optional params model properties can be populated from either path holes or query string values when a URL is matched. However, **only leaf-level view models** — those with no child routes registered under them — receive query string values. The query string is consumed entirely by the deepest (leaf) view model in the route hierarchy.

This means that any view model with registered children must satisfy all its required properties through path holes alone. Optional properties on non-leaf view models can only be provided via additional patterns in a [route group](#route-groups) that place the value in a path hole. The navigator validates these constraints at build time and throws an exception if:

- A route to a view model with registered children doesn't set all required properties in path holes.
- A view model with registered children has a `RouteQuery` parameter type or a params model with a `RouteQuery` property.

> [!TIP]
> If a parent view model needs access to query string values that only its leaf child receives, have the parent implement an interface and let the child pass the values up through it. See [Dependency Injection](dependency-injection.md) for techniques like the presenter interface pattern and ancestor interface injection.

The route builder references the model's properties in the URL template:

```csharp
public static ChildRoutePart<RepoViewModel, DocumentViewModel, DocumentParams> DocumentChild { get; } =
    Route.Build((DocumentParams p) => $"document/{p.DocumentId}/v/{p.VersionId}")
         .Child<RepoViewModel, DocumentViewModel>();
```

The view model receives the whole model through `this.Parameter`:

```csharp
public partial class DocumentViewModel : ObservableObject, IRoutedViewModel<DocumentParams>
{
    public long DocumentId => this.Parameter.DocumentId;
    public long VersionId => this.Parameter.VersionId;
}
```

> [!NOTE]
> `this.Parameter` is available immediately — you can read it from the view model's constructor if desired, not only after navigation lifecycle events fire.

### Raw Query String

If your view model just needs arbitrary query parameters without a fixed structure, use `RouteQuery` directly as the parameter type. The parameter is not referenced in the path, so pass the route template as a plain string.

> [!NOTE]
> `RouteQuery` — whether used directly as a parameter type or as a property in a params model — is only available for [leaf view models](#query-string-and-leaf-view-models) (those with no child routes registered).

```csharp
public static ChildRoutePart<MainViewModel, SearchViewModel, RouteQuery> SearchChild { get; } =
    Route.Build("search").Child<MainViewModel, SearchViewModel>();
```

```csharp
public partial class SearchViewModel : ObservableObject, IRoutedViewModel<RouteQuery>
{
    public Task OnNavigatedToAsync(NavigationArgs args)
    {
        if (this.Parameter.TryGetValue("q", out string? term))
        {
            // ...
        }

        return Task.CompletedTask;
    }
}
```

### Routes Without Path Parameters

Any time the view model's parameter type is not represented in the path — no parameter at all, a `RouteQuery`, or a params model whose only properties are query values or nullable path holes that no current pattern fills — pass the template as a plain string to `Route.Build(...)` or `Route.BuildGroup<T>().Add(...)` instead of using a lambda:

```csharp
Route.Build("/settings").Root<SettingsViewModel>();

Route.BuildGroup<DocumentParams>()
    .Add("document")
    .Add(p => $"document/{p.DocumentId}")
    .Child<RepoViewModel, DocumentViewModel>();
```

### Optional Single-Parsable Parameters

When a view model's parameter is a single parsable type (rather than a params model) and it participates in a [route group](#route-groups) where some patterns supply the value in the path and others don't, wrap the type in `OptionalPathParam<T>`:

```csharp
public partial class DocumentViewModel : ObservableObject, IRoutedViewModel<OptionalPathParam<long>>
{
    public long? DocumentId => this.Parameter.AsNullable();
}

public static ChildRoutePart<MainViewModel, DocumentViewModel, OptionalPathParam<long>> DocumentChild { get; } =
    Route.BuildGroup<OptionalPathParam<long>>()
        .Add("document")
        .Add(id => $"document/{id}")
        .Child<MainViewModel, DocumentViewModel>();
```

View model parameter types are constrained to `notnull` — both for AOT safety and because reference-type nullability (`T?` on a class) cannot be observed at runtime. `OptionalPathParam<T>` is a `Nullable<>`-style wrapper that works for value types and reference types alike while satisfying the `notnull` constraint. Call `AsNullable()` on the parameter to convert it to a plain nullable value when that is more convenient to work with.

This wrapper is only needed for single-parsable parameters. Params models simply use nullable property types — see [Params Models](#params-models) above.

## Route Groups

A route group defines multiple URL patterns that all map to the same view model and parameter type. Use `Route.BuildGroup<TParams>()` and chain `Add(...)` calls for each pattern:

```csharp
[RouteParamsModel]
public partial record DocumentParams
{
    public required long DocumentId { get; init; }
    public long? VersionId { get; init; }
}

public static ChildRoutePart<RepoViewModel, DocumentViewModel, DocumentParams> DocumentChild { get; } =
    Route.BuildGroup<DocumentParams>()
        .Add(p => $"document/{p.DocumentId}")
        .Add(p => $"document/{p.DocumentId}/v/{p.VersionId}")
        .Child<RepoViewModel, DocumentViewModel>();
```

Navigating to `/document/42` produces a `DocumentParams` with `VersionId = null`; navigating to `/document/42/v/7` produces one with `VersionId = 7`. Nullable properties may appear in some patterns and be omitted from others. For non-leaf view models, required (non-nullable) properties must appear in every pattern as path holes; for leaf view models, required properties that aren't placed in holes can still be satisfied by query string values (see [Query String and Leaf View Models](#query-string-and-leaf-view-models)).

**Matching an incoming URL**: patterns are tried in declared order, and the first one whose path structure matches and whose values successfully parse wins. List more specific patterns before more general ones when both could match the same URL.

**Generating a URL** from a parameters object (e.g. when calling `ToConcrete(p)`): the pattern that consumes the most properties as path holes wins. Any properties that the chosen pattern doesn't place in the path are appended as query string values.

## The Routes Class Pattern

A conventional `Routes` class in the view model project centralizes route definitions and provides an `AddAllRoutes` extension method used during navigator configuration:

```csharp
using System.ComponentModel;
using Singulink.UI.Navigation;

namespace MyApp.ViewModels;

[Bindable(true)]
public static class Routes
{
    public static RootRoutePart<LoginViewModel> LoginRoot { get; } =
        Route.Build("/login").Root<LoginViewModel>();

    public static RootRoutePart<MainViewModel> MainRoot { get; } =
        Route.Build("/").Root<MainViewModel>();

    public static RootRoutePart<RepoViewModel, string> RepoRoot { get; } =
        Route.Build((string repoId) => $"r/{repoId}").Root<RepoViewModel>();

    public static RepoRoutes Repo { get; } = new();

    public class RepoRoutes
    {
        public ChildRoutePart<RepoViewModel, HomePageViewModel> HomePage { get; } =
            Route.Build("home").Child<RepoViewModel, HomePageViewModel>();

        public ChildRoutePart<RepoViewModel, SettingsPageViewModel> SettingsPage { get; } =
            Route.Build("settings").Child<RepoViewModel, SettingsPageViewModel>();

        public ChildRoutePart<RepoViewModel, DocumentPageViewModel, long> DocumentPage { get; } =
            Route.Build((long id) => $"doc/{id}").Child<RepoViewModel, DocumentPageViewModel>();
    }

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRoute(LoginRoot);
        builder.AddRoute(MainRoot);
        builder.AddRoute(RepoRoot);

        builder.AddRoute(Repo.HomePage);
        builder.AddRoute(Repo.SettingsPage);
        builder.AddRoute(Repo.DocumentPage);
    }
}
```

### Why a nested instance class (`RepoRoutes`)?

XAML `{x:Bind}` can traverse **static property → instance property** chains like `Routes.Repo.HomePage`, but it cannot traverse static properties on a static nested type (`Routes.Repo.HomePage` only works if `Repo` is a static property that returns an instance of `RepoRoutes`, not if `Repo` is a nested static class). Exposing child routes as instance properties on a small nested class lets XAML bind to them directly:

```xml
<Button Content="Home"
        Command="{x:Bind Model.NavigateToCommand}"
        CommandParameter="{x:Bind vm:Routes.Repo.HomePage}" />
```

### Why `[Bindable(true)]`?

On Uno Platform, the `[Bindable]` attribute ensures the class is discoverable by the runtime data binding engine. It is harmless on pure WinUI and recommended any time a class (or its properties) is referenced from XAML data bindings.

## Registering Routes

Every route must be registered with the navigator builder via `AddRoute`. Child routes must be added after their parent has been added. The `AddAllRoutes` extension convention shown above keeps the registration order in one place and is called from the navigator build action:

```csharp
_navigator = new Navigator(rootContent, builder => {
    builder.MapRoutedView<LoginViewModel, LoginPage>();
    builder.MapRoutedView<MainViewModel, MainPage>();
    builder.MapRoutedView<RepoViewModel, RepoPage>();
    builder.MapRoutedView<HomePageViewModel, HomePageView>();
    // ...

    builder.AddAllRoutes();
});
```

See [WinUI / Uno Setup](winui-setup.md) for full navigator configuration and [Routed View Models and Lifecycle](view-models.md) for authoring the view models themselves.

</div>
