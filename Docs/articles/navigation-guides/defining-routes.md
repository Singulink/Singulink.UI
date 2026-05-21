<div class="article">

# Defining Routes

Routes describe the hierarchy of your application as a tree of strongly-typed route parts. Each route part maps to a view model, and together they form the URL structure of your app.

## Route Parts

There are two kinds of route parts:

- **Root route parts**: top-level routes without a parent (e.g. `/login`, `/`, `/r/{repoId}`).
- **Child route parts**: routes that appear under a specific parent view model (e.g. `home` under a repository root).

Root and child routes are built with the <xref:Singulink.UI.Navigation.Route.Build*> fluent API:

```csharp
using Singulink.UI.Navigation;

public static RootRoutePart<LoginViewModel> LoginRoot { get; } =
    Route.Build("/login").Root<LoginViewModel>();

public static ChildRoutePart<MainViewModel, HomeViewModel> HomeChild { get; } =
    Route.Build("home").Child<MainViewModel, HomeViewModel>();
```

The generic arguments make these strongly typed: a <xref:Singulink.UI.Navigation.ChildRoutePart`2> can only be registered under a parent route that maps to `MainViewModel`.

## View Model Parameters

A view model can accept a single **parameter** which is populated from the URL. The parameter type can be one of three things:

1. **A [params model](#params-models)**: a record annotated with <xref:Singulink.UI.Navigation.RouteParamsModelAttribute>, letting you pack multiple path and query values into one strongly-typed object.
2. **A single parsable type**: any type that implements <xref:System.IParsable`1> and <xref:System.IEquatable`1> (e.g. `int`, `long`, `Guid`, `string`).
3. **<xref:Singulink.UI.Navigation.RouteQuery>**: if you only want a raw query string (e.g. arbitrary filter parameters), pass <xref:Singulink.UI.Navigation.RouteQuery> directly.

#### Single Parameter

The simplest case is a single value parsed from the path:

```csharp
public static RootRoutePart<RepoViewModel, string> RepoRoot { get; } =
    Route.Build((string repoId) => $"r/{repoId}").Root<RepoViewModel>();

public static ChildRoutePart<RepoViewModel, DocumentViewModel, long> DocumentChild { get; } =
    Route.Build((long documentId) => $"document/{documentId}").Child<RepoViewModel, DocumentViewModel>();
```

The lambda describes the URL template using string interpolation; the parameters in the lambda are placeholders substituted into the path. At runtime, URL segments are parsed into the declared type.

The corresponding view model declares <xref:Singulink.UI.Navigation.IRoutedViewModel`1>:

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

The `[RouteParamsModel]` source generator implements <xref:Singulink.UI.Navigation.IRouteParamsModel`1> on the record, enabling bidirectional conversion between the record and a URL.

**Rules for params models**:

- Must be a `partial record`.
- All properties must be `init`-only.
- Non-nullable properties must be marked `required` (or be primary constructor parameters).
- Property types must be parsable (<xref:System.IParsable`1> + <xref:System.IEquatable`1>).
- Nullable properties correspond to values that may or may not be present. They can be populated from query string values (on [leaf view models](#query-string-and-leaf-view-models)) or from path holes in a [route group](#route-groups) where some patterns fill the hole and others omit it.
- At most one <xref:Singulink.UI.Navigation.RouteQuery> property is allowed. It is optional, can be named anything (`Query`, `Rest`, `Extras`, ...), and captures any query string values that don't match another property in the model.

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

If your view model just needs arbitrary query parameters without a fixed structure, use <xref:Singulink.UI.Navigation.RouteQuery> directly as the parameter type. The parameter is not referenced in the path, so pass the route template as a plain string.

> [!NOTE]
> <xref:Singulink.UI.Navigation.RouteQuery> (whether used directly as a parameter type or as a property in a params model) is only available for [leaf view models](#query-string-and-leaf-view-models) (those with no child routes registered).

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

Any time the view model's parameter type is not represented in the path (no parameter at all, a <xref:Singulink.UI.Navigation.RouteQuery>, or a params model whose only properties are query values or nullable path holes that no current pattern fills), pass the template as a plain string to <xref:Singulink.UI.Navigation.Route.Build*> or <xref:Singulink.UI.Navigation.Route.BuildGroup``1> instead of using a lambda:

```csharp
Route.Build("/settings").Root<SettingsViewModel>();

Route.Build<QueryString>("search").Child<MainViewModel, SearchViewModel>();

Route.BuildGroup<DocumentParams>()
    .Add("document")
    .Add(p => $"document/{p.DocumentId}")
    .Child<RepoViewModel, DocumentViewModel>();
```

#### Optional Single Parameters

When a view model's parameter is a single parsable type (rather than a params model) and it participates in a [route group](#route-groups) where some patterns supply the value in the path and others don't, wrap the type in <xref:Singulink.UI.Navigation.OptionalPathParam`1>:

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

View model parameter types are constrained to `notnull`, both for technical AOT safety reasons and because reference-type nullability (`T?` on a class) cannot be observed at runtime. <xref:Singulink.UI.Navigation.OptionalPathParam`1> is a <xref:System.Nullable`1>-style wrapper that works for value types and reference types alike while satisfying the `notnull` constraint and maintaining AOT compatible parsing. Call <xref:Singulink.UI.Navigation.OptionalPathParamValueExtensions.ToNullable*> on the parameter to convert it to a plain nullable value when that is more convenient to work with.

This wrapper is only needed for single-parsable parameters. Params models simply use nullable property types (see [Params Models](#params-models) above).

### Lists of Values

<xref:Singulink.UI.Navigation.ValueList`1> lets you use a list of parsable values anywhere a single parsable type is expected, whether as a path parameter, a query parameter, or a property in a params model. It implements <xref:System.IParsable`1> and <xref:System.IEquatable`1>, so it satisfies the constraints required by route parameters and <xref:Singulink.UI.Navigation.RouteQuery> accessors.

```csharp
[RouteParamsModel]
public partial record FilterParams
{
    public ValueList<long> Ids { get; init; }
    public ValueList<string>? Tags { get; init; }
}
```

Because <xref:Singulink.UI.Navigation.ValueList`1> participates in standard parsing, it works in path holes too:

```csharp
public static RootRoutePart<BatchViewModel, ValueList<long>> BatchRoot { get; } =
    Route.Build((ValueList<long> ids) => $"batch/{ids}").Root<BatchViewModel>();
```

The string format is URI-safe and round-trippable:

- **Tilde-separated** (e.g. `~1~2~3`): used when no value contains a tilde.
- **Length-prefixed** (e.g. `5~hello5~world`): used when any value contains a tilde, or as a safe fallback.

The format is a serialization detail; you typically don't construct or parse it manually. Build a <xref:Singulink.UI.Navigation.ValueList`1> from items:

```csharp
ValueList<long> ids = new(1L, 2L, 3L);
ValueList<string> tags = ImmutableArray.Create("a", "b");      // implicit conversion
ValueList<long> fromList = new(someEnumerable);
```

<xref:Singulink.UI.Navigation.ValueList`1> implements <xref:System.Collections.Generic.IReadOnlyList`1> and provides implicit conversions from <xref:System.Collections.Immutable.ImmutableArray`1> / to <xref:System.Collections.Immutable.ImmutableArray`1>, <xref:System.ReadOnlySpan`1>, and <xref:System.ReadOnlyMemory`1> (no copying), plus <xref:Singulink.UI.Navigation.ValueList`1.AsSpan>, <xref:Singulink.UI.Navigation.ValueList`1.AsMemory>, and <xref:Singulink.UI.Navigation.ValueList`1.ToArray> helpers.

## Working with Query Strings

Query strings are represented by the <xref:Singulink.UI.Navigation.RouteQuery> type, an immutable, insertion-ordered, key/value collection of strongly-typed parameters. Three patterns use it:

- A view model whose parameter type is <xref:Singulink.UI.Navigation.RouteQuery> directly, for arbitrary query parameters with no fixed structure.
- A params model with a single <xref:Singulink.UI.Navigation.RouteQuery> property, for view models that need both fixed properties and arbitrary leftover query values.
- Manually-constructed query strings passed to <xref:Singulink.UI.Navigation.RootRoutePart`2.ToConcrete*> for navigation.

##### Query String and Leaf View Models

Both required and optional params model properties can be populated from either path holes or query string values when a URL is matched. However, **only leaf-level view models** (those with no child routes registered under them) receive query string values. The query string is consumed entirely by the deepest (leaf) view model in the route hierarchy.

This means that any view model with registered children must satisfy all its required properties through path holes alone. Optional properties on non-leaf view models can only be provided via additional patterns in a [route group](#route-groups) that place the value in a path hole. The navigator validates these constraints at build time and throws an exception if:

- A route to a view model with registered children doesn't set all required properties in path holes.
- A view model with registered children has a <xref:Singulink.UI.Navigation.RouteQuery> parameter type or a params model with a <xref:Singulink.UI.Navigation.RouteQuery> property.

> [!TIP]
> If a parent view model needs access to query string values that only its leaf child receives, have the parent implement an interface and let the child pass the values up through it. See [Dependency Injection](dependency-injection.md) for techniques like the presenter interface pattern and ancestor interface injection.

#### Reading Values from a RouteQuery

<xref:Singulink.UI.Navigation.RouteQuery> parses values lazily; values are stored as strings and converted to your requested type on access using invariant-culture formatting (the same formatting used for path parameters):

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

<xref:Singulink.UI.Navigation.RouteQuery.TryGetValue*> has additional overloads that can distinguish a missing key from a parse failure or throw on parse errors instead of returning `false`. The latter is useful when you want missing values to be tolerated but malformed values to be loud.

Reading a <xref:Singulink.UI.Navigation.ValueList`1> from a <xref:Singulink.UI.Navigation.RouteQuery> works just like any other parsable type:

```csharp
if (this.Parameter.TryGetValue("ids", out ValueList<long> ids))
{
    foreach (long id in ids) { ... }
}
```

<xref:Singulink.UI.Navigation.RouteQuery> is enumerable (yielding `(string Key, string Value)` tuples) and exposes <xref:Singulink.UI.Navigation.RouteQuery.Count>, supporting iteration over all entries.

#### Building a RouteQuery

For static / known-at-compile-time query parameters, use the <xref:Singulink.UI.Navigation.RouteQuery> constructor directly:

```csharp
var query = new RouteQuery(("q", "hello"), ("page", "2"));
```

Note that the constructor takes pre-formatted string values. For strongly-typed building, use <xref:Singulink.UI.Navigation.RouteQueryBuilder>:

```csharp
var query = new RouteQueryBuilder()
    .Add("q", "hello")        // string
    .Add("page", 2)           // int, formatted with invariant culture
    .Add("since", DateOnly.FromDateTime(DateTime.UtcNow))
    .ToQuery();
```

<xref:Singulink.UI.Navigation.RouteQueryBuilder> provides <xref:Singulink.UI.Navigation.RouteQueryBuilder.Add*> (throws on duplicate key), <xref:Singulink.UI.Navigation.RouteQueryBuilder.Set*> (overwrites), <xref:Singulink.UI.Navigation.RouteQueryBuilder.Remove*>, <xref:Singulink.UI.Navigation.RouteQueryBuilder.ContainsKey(System.String)>, and <xref:Singulink.UI.Navigation.RouteQueryBuilder.TryGetValue*>. To start a builder from an existing query, call <xref:Singulink.UI.Navigation.RouteQuery.ToBuilder> on the query.

#### Navigating with a Query

When the view model's parameter type is <xref:Singulink.UI.Navigation.RouteQuery>, pass the query directly to <xref:Singulink.UI.Navigation.RootRoutePart`2.ToConcrete*>:

```csharp
public static RootRoutePart<SearchViewModel, RouteQuery> SearchRoot { get; } =
    Route.Build<RouteQuery>("/search").Root<SearchViewModel>();

// Navigate:
var query = new RouteQueryBuilder().Add("q", "hello").Add("page", 2).ToQuery();
await this.Navigator.NavigateAsync(Routes.SearchRoot.ToConcrete(query));
// URL: /search?q=hello&page=2
```

When the parameter is a params model, place the <xref:Singulink.UI.Navigation.RouteQuery> on a property:

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

The <xref:Singulink.UI.Navigation.RouteQuery> property captures any query string values that don't match another property in the model. See [Params Models](#params-models) for the full rules.

## Route Groups

A route group defines multiple URL patterns that all map to the same view model and parameter type. Use <xref:Singulink.UI.Navigation.Route.BuildGroup``1> and chain `Add(...)` calls for each pattern:

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

**Generating a URL** from a parameters object (e.g. when calling <xref:Singulink.UI.Navigation.RootRoutePart`2.ToConcrete*> with the parameter): the pattern that consumes the most properties as path holes wins. Any properties that the chosen pattern doesn't place in the path are appended as query string values.

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

### Registering Routes

Every route must be registered with the navigator builder via <xref:Singulink.UI.Navigation.INavigatorBuilder.AddRoute(Singulink.UI.Navigation.RoutePart)>. Parent routes must be added before their children. The `AddAllRoutes` extension convention shown above keeps the registration order in one place and is called from the navigator build action:

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
