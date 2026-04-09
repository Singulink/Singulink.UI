<div class="article">

# Parent Views and Child Navigation

A parent route doesn't just "contain" its children conceptually — in WinUI/Uno it **hosts** them. The parent view stays mounted while child routes swap in and out of a child content host. This guide shows how to set up parent views correctly.

## The `IParentView` Interface

Any view that serves as a parent in the route hierarchy must implement `IParentView`:

```csharp
using Microsoft.UI.Xaml.Controls;
using Singulink.UI.Navigation.WinUI;

namespace MyApp.Client.Views;

public sealed partial class RepoPage : Page, IParentView
{
    public RepoViewModel Model => (RepoViewModel)DataContext;

    public RepoPage() => InitializeComponent();

    public ViewNavigator CreateChildViewNavigator() => ViewNavigator.Create(MainContent);
}
```

`CreateChildViewNavigator` returns a `ViewNavigator` built around a content control where the child view will be hosted. The navigator calls this method once per materialization of the parent view — typically the first time the parent is navigated to.

The builder validates at startup that any view model registered as a parent has a view type implementing `IParentView`, so you cannot forget to implement it.

## The Parent's XAML

A parent view's XAML typically reserves a named container for child content, alongside any parent-level chrome (navigation bars, headers, etc.):

```xml
<Page
    x:Class="MyApp.Client.Views.RepoPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:MyApp.ViewModels">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Header / navigation chrome -->
        <Grid Grid.Row="0" Padding="20,10">
            <StackPanel Orientation="Horizontal" Spacing="10">
                <Button Content="Home"
                        Command="{x:Bind Model.NavigateToCommand}"
                        CommandParameter="{x:Bind vm:Routes.Repo.HomePage}" />
                <Button Content="Settings"
                        Command="{x:Bind Model.NavigateToCommand}"
                        CommandParameter="{x:Bind vm:Routes.Repo.SettingsPage}" />
            </StackPanel>
        </Grid>

        <!-- Child view host -->
        <ContentControl x:Name="MainContent"
                        Grid.Row="1"
                        HorizontalContentAlignment="Stretch"
                        VerticalContentAlignment="Stretch" />
    </Grid>
</Page>
```

The child host is typically a `ContentControl` with stretched content alignment, but any control that `ViewNavigator.Create` accepts works.

## How Child Navigation Works

When a child route is navigated to, the navigator:

1. Materializes the parent view and its view model (if not already active).
2. Calls `IParentView.CreateChildViewNavigator()` on the parent view to get the child host.
3. Materializes the child view and view model.
4. Sets the child view as the active view inside the parent's child navigator.

When navigating between sibling child routes under the same parent, only step 3 and 4 happen — the parent view stays mounted and receives `OnRouteNavigatedAsync` notifications (see [Routed View Models and Lifecycle](view-models.md)).

## Multi-Level Parents

The same pattern composes recursively. A child view can itself be a parent by implementing `IParentView`. Its `CreateChildViewNavigator` returns a view navigator for its own grandchild content host. There is no depth limit — each level participates in the same materialize-once-and-swap-children lifecycle.

## Navigation Commands from the Parent

A common convention is for the parent view model to expose a `NavigateToCommand` that accepts a child route part from XAML:

```csharp
public partial class RepoViewModel : ObservableObject, IRoutedViewModel<string>
{
    [RelayCommand]
    private async Task NavigateToAsync(IConcreteChildRoutePart<RepoViewModel> childRoutePart)
        => await this.Navigator.NavigatePartialAsync(childRoutePart);
}
```

Combined with the [Routes class pattern](defining-routes.md#the-routes-class-pattern), this gives you strongly-typed navigation buttons throughout the parent view without any magic strings.

</div>
