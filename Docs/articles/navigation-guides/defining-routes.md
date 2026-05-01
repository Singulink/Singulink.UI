<div class="article">

# Defining Routes

### Overview

Routes describe the hierarchy of your application as a tree of strongly-typed route parts. Each route part maps to a view model, and together they form the URL structure of your app.

## Route Parts

There are two kinds of route parts:

- **Root route parts**: top-level routes without a parent (e.g. `/login`, `/`, `/r/{repoId}`).
- **Child route parts**: routes that appear under a specific parent view model (e.g. `home` under a repository root).

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

1. **A [params model](#params-models)**: a record annotated with `[RouteParamsModel]`, letting you pack multiple path and query values into one strongly-typed object.
2. **A single parsable type**: any type that implements `IParsable<T>` and `IEquatable<T>` (e.g. `int`, `long`, `Guid`, `string`).
3. **`RouteQuery`**: if you only want a raw query string (e.g. arbitrary filter parameters), pass `RouteQuery` directly.

#### Single Parameter

The simplest case is a single value parsed from the path:

```csharp
public static RootRoutePart<RepoViewModel, string> RepoRoot { get; } =
    Route.Build((string repoId) => $"r/{repoId}").Root<RepoViewModel>();

public static ChildRoutePart<RepoViewModel, DocumentViewModel, long> DocumentChild { get; } =
    Route.Build((long documentId) => $"document/{documentId}").Child<RepoViewModel, DocumentViewModel>();
```

The lambda describes the URL template using string interpolation; the parameters in the lambda are placeholders substituted into the path. At runtime, URL segments are parsed into the declared type.

The corresponding view model declares `IRoutedViewModel<T>`:

```csharp
public partial class DocumentViewModel : ObservableObject, IRoutedViewModel<long>
{
    public long DocumentId => this.Parameter;

    public Task OnNavigatedToAsync(NavigationArgs args) { ... }
}
```

#### Params Models

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

##### Query String and Leaf View Models

Both required and optional params model properties can be populated from either path holes or query string values when a URL is matched. However, **only leaf-level view models** (those with no child routes registered under them) receive query string values. The query string is consumed entirely by the deepest (leaf) view model in the route hierarchy.

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
> `this.Parameter` can be accessed any time, even in the view model's constructor - you do not need to wait until a navigation event fires.

#### Raw Query String

If your view model just needs arbitrary query parameters without a fixed structure, use `RouteQuery` directly as the parameter type. The parameter is not referenced in the path, so pass the route template as a plain string.

> [!NOTE]
> `RouteQuery` (whether used directly as a parameter type or as a property in a params model) is only available for [leaf view models](#query-string-and-leaf-view-models) (those with no child routes registered).

```csharp
public static ChildRoutePart<MainViewModel, SearchViewModel, RouteQuery> SearchChild { get; } =
    Route.Build<RouteQuery>("search").Child<MainViewModel, SearchViewModel>();
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

#### Routes Without Path Parameters

Any time the view model's parameter type is not represented in the path (no parameter at all, a `RouteQuery`, or a params model whose only properties are query values or nullable path holes that no current pattern fills), pass the template as a plain string to `Route.Build(...)` or `Route.BuildGroup<T>().Add(...)` instead of using a lambda:

```csharp
Route.Build("/settings").Root<SettingsViewModel>();

Route.Build<QueryString>("search").Child<MainViewModel, SearchViewModel>();

Route.BuildGroup<DocumentParams>()
    .Add("document")
    .Add(p => $"document/{p.DocumentId}")
    .Child<RepoViewModel, DocumentViewModel>();
```

#### Optional Single Parameters

When a view model's parameter is a single parsable type (rather than a params model) and it participates in a [route group](#route-groups) where some patterns supply the value in the path and others don't, wrap the type in `OptionalPathParam<T>`:

```csharp
public partial class DocumentViewModel : ObservableObject, IRoutedViewModel<OptionalPathParam<long>>
{
    public long? DocumentId => this.Parameter.ToNullable();
}

public static ChildRoutePart<MainViewModel, DocumentViewModel, OptionalPathParam<long>> DocumentChild { get; } =
    Route.BuildGroup<OptionalPathParam<long>>()
        .Add("document")
        .Add(id => $"document/{id}")
        .Child<MainViewModel, DocumentViewModel>();
```

View model parameter types are constrained to `notnull`, both for technical AOT safety reasons and because reference-type nullability (`T?` on a class) cannot be observed at runtime. `OptionalPathParam<T>` is a `Nullable<>`-style wrapper that works for value types and reference types alike while satisfying the `notnull` constraint and maintaining AOT compatible parsing. Call `ToNullable()` on the parameter to convert it to a plain nullable value when that is more convenient to work with.

This wrapper is only needed for single-parsable parameters. Params models simply use nullable property types (see [Params Models](#params-models) above).

## Working with Query Strings

Query strings are represented by the `RouteQuery` type, an immutable, insertion-ordered, key/value collection of strongly-typed parameters. Three patterns use it:

- A view model whose parameter type is `RouteQuery` directly, for arbitrary query parameters with no fixed structure.
- A params model with a single `RouteQuery` property, for view models that need both fixed properties and arbitrary leftover query values.
- Manually-constructed query strings passed to `ToConcrete(...)` for navigation.

Recall from [Query String and Leaf View Models](#query-string-and-leaf-view-models) that query strings are only available on leaf view models; view models with registered children must satisfy all required parameters from path holes.

#### Reading Values from a RouteQuery

`RouteQuery` parses values lazily; values are stored as strings and converted to your requested type on access using invariant-culture formatting (the same formatting used for path parameters):

```csharp
public Task OnNavigatedToAsync(NavigationArgs args)
{
    if (this.Parameter.TryGetValue("q", out string? term))
    {
        // Found and parsed.
    }

    if (this.Parameter.TryGetValue("page", out int page))
    {
        CurrentPage = page;
    }

    // Throws KeyNotFoundException if missing, FormatException if not parsable as int:
    int requiredId = this.Parameter.GetValue<int>("id");

    // Check existence without parsing:
    if (this.Parameter.ContainsKey("debug"))
    {
        // ...
    }

    return Task.CompletedTask;
}
```

`TryGetValue<T>` has an overload that distinguishes a missing key from a parse failure via an `out bool foundKey`, and another that throws on parse errors instead of returning `false`. The latter is useful when you want missing values to be tolerated but malformed values to be loud.

`RouteQuery` is enumerable (yielding `(string Key, string Value)` tuples) and exposes `Count`, supporting iteration over all entries.

#### Building a RouteQuery

For static / known-at-compile-time query parameters, use the `RouteQuery` constructor directly:

```csharp
var query = new RouteQuery(("q", "hello"), ("page", "2"));
```

Note that the constructor takes pre-formatted string values. For strongly-typed building, use `RouteQueryBuilder`:

```csharp
var query = new RouteQueryBuilder()
    .Add("q", "hello")        // string
    .Add("page", 2)           // int, formatted with invariant culture
    .Add("since", DateOnly.FromDateTime(DateTime.UtcNow))
    .ToQuery();
```

`RouteQueryBuilder` provides `Add` (throws on duplicate key), `Set` (overwrites), `Remove`, `ContainsKey`, and `TryGetValue<T>`. To start a builder from an existing query, call `existingQuery.ToBuilder()`.

#### Navigating with a Query

When the view model's parameter type is `RouteQuery`, pass the query directly to `ToConcrete`:

```csharp
public static RootRoutePart<SearchViewModel, RouteQuery> SearchRoot { get; } =
    Route.Build("/search").Root<SearchViewModel, RouteQuery>();

// Navigate:
var query = new RouteQueryBuilder().Add("q", "hello").Add("page", 2).ToQuery();
await this.Navigator.NavigateAsync(Routes.SearchRoot.ToConcrete(query));
// URL: /search?q=hello&page=2
```

When the parameter is a params model, place the `RouteQuery` on a property:

```csharp
[RouteParamsModel]
public partial record SearchParams
{
    public required string Term { get; init; }
    public RouteQuery Filters { get; init; }
}

await this.Navigator.NavigateAsync(Routes.SearchRoot.ToConcrete(new SearchParams
{
    Term = "hello",
    Filters = new RouteQueryBuilder().Add("category", "books").Add("inStock", true).ToQuery(),
}));
```

The `RouteQuery` property captures any query string values that don't match another property in the model. See [Params Models](#params-models) for the full rules.

### Lists of Values

`ValueList<T>` lets you use a list of parsable values anywhere a single parsable type is expected, whether as a path parameter, a query parameter, or a property in a params model. It implements `IParsable<T>` and `IEquatable<T>`, so it satisfies the constraints required by route parameters and `RouteQuery` accessors.

```csharp
[RouteParamsModel]
public partial record FilterParams
{
    public ValueList<long> Ids { get; init; }
    public ValueList<string>? Tags { get; init; }
}
```

Because `ValueList<T>` participates in standard parsing, it works in path holes too:

```csharp
public static RootRoutePart<BatchViewModel, ValueList<long>> BatchRoot { get; } =
    Route.Build((ValueList<long> ids) => $"batch/{ids}").Root<BatchViewModel>();
```

The string format is URI-safe and round-trippable:

- **Tilde-separated** (e.g. `~1~2~3`): used when no value contains a tilde.
- **Length-prefixed** (e.g. `5~hello5~world`): used when any value contains a tilde, or as a safe fallback.

The format is a serialization detail; you typically don't construct or parse it manually. Build a `ValueList<T>` from items:

```csharp
ValueList<long> ids = new(1L, 2L, 3L);
ValueList<string> tags = ImmutableArray.Create("a", "b");      // implicit conversion
ValueList<long> fromList = new(someEnumerable);
```

`ValueList<T>` implements `IReadOnlyList<T>` and provides implicit conversions from `ImmutableArray<T>` / to `ImmutableArray<T>`, `ReadOnlySpan<T>`, and `ReadOnlyMemory<T>` (no copying), plus `AsSpan()`, `AsMemory()`, and `ToArray()` helpers.

Reading a `ValueList<T>` from a `RouteQuery` works just like any other parsable type:

```csharp
if (this.Parameter.TryGetValue("ids", out ValueList<long> ids))
{
    foreach (long id in ids) { ... }
}
```

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

##### Why a nested instance class (RepoRoutes)?

XAML `{x:Bind}` can traverse **static property → instance property** chains like `Routes.Repo.HomePage`, but a limitation of the WinUI XAML dialect is that it cannot traverse static properties on a static nested type (`Routes.Repo.HomePage` only works if `Repo` is a static property that returns an instance of `RepoRoutes`, not if `Repo` is a nested static class). Exposing child routes as instance properties on a small nested class lets XAML bind to them directly:

```xml
<Button Content="Home"
        Command="{x:Bind Model.NavigateToCommand}"
        CommandParameter="{x:Bind vm:Routes.Repo.HomePage}" />
```

Flattening the class structure with a different naming convention (e.g. `Routes.RepoHomePage`, `Routes.RepoSettingsPage`, etc) also works, or if you don't need to `{x:Bind}` directly to routes then you can use normal nested static classes.

## Registering Routes

Every route must be registered with the navigator builder via `AddRoute`. Parent routes must be added before their children. The `AddAllRoutes` extension convention shown above keeps the registration order in one place and is called from the navigator build action:

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
