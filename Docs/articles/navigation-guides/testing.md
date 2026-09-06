<div class="article">

# Testing View Models

View models built on the framework never depend on a UI framework, but they do depend on a navigator: routed view models get one when the navigator creates them, and dialog view models get one when they are shown. The **Singulink.UI.Navigation.Testing** package provides <xref:Singulink.UI.Navigation.Testing.TestNavigator>, an in-memory navigator that runs the real navigation and dialog engine against placeholder views, so view models are tested exactly as they run in the application, without WinUI or Uno.

Reference the package from your test project:

```xml
<PackageReference Include="Singulink.UI.Navigation.Testing" Version="..." />
```

## The Test Context

The navigator and its task runners require a single-threaded synchronization context, just like a UI thread. Wrap each test body in <xref:Singulink.UI.Navigation.Testing.NavigationTestContext.Run*>, which provides one and runs all continuations and fire-and-forget work on the test thread in order:

```csharp
[TestMethod]
public void Delete_ShowsConfirmation()
{
    NavigationTestContext.Run(async () =>
    {
        var nav = new TestNavigator(b => { ... });
        // ...
    });
}
```

Any exception thrown by queued work, including work started with <xref:Singulink.UI.Tasks.TaskRunner.RunAndForget*>, fails the test.

## Building a Test Navigator

Map view models with <xref:Singulink.UI.Navigation.Testing.TestNavigatorBuilder.MapViewModel*> and add the same route definitions the application uses. No views are involved: the test navigator maps every view model to a placeholder.

```csharp
var nav = new TestNavigator(b =>
{
    b.MapViewModel<RepoRootModel>();
    b.MapViewModel<ViewFolderPageModel>();

    b.AddRoute(Routes.Repo);
    b.AddRoute(Routes.ViewFolder);

    b.Services = serviceProvider; // Optional: constructor injection for view models
});
```

View models are created the same way the application navigator creates them, so constructor injection from <xref:Singulink.UI.Navigation.NavigatorBuilderCore.Services> and parent-provided child services work, and `this.Navigator` is available inside constructors.

## Navigating and Inspecting State

Navigate with the normal <xref:Singulink.UI.Navigation.INavigator> API and inspect the result:

```csharp
await nav.NavigateAsync("repo/demo/folders/42");

nav.LastNavigationResult.ShouldBe(NavigationResult.Success);
var page = nav.ActiveViewModel<ViewFolderPageModel>();
page.Parameter.ShouldBe(42);
```

- <xref:Singulink.UI.Navigation.Testing.TestNavigator.ActiveViewModels> lists the active view models from the root of the view hierarchy to the leaf.
- <xref:Singulink.UI.Navigation.Testing.TestNavigator.ActiveViewModel*> returns the active view model of a given type.
- <xref:Singulink.UI.Navigation.Testing.TestNavigator.ShowingDialogs> and <xref:Singulink.UI.Navigation.Testing.TestNavigator.TopDialog> expose the dialog stack.
- Everything on <xref:Singulink.UI.Navigation.INavigator> works as usual: <xref:Singulink.UI.Navigation.INavigator.CurrentRoute>, <xref:Singulink.UI.Navigation.INavigator.CanGoBack>, <xref:Singulink.UI.Navigation.INavigator.GetBackStack*>, <xref:Singulink.UI.Navigation.IDialogPresenter.CanShowDialog> and so on.

## The Event Journal

<xref:Singulink.UI.Navigation.Testing.TestNavigator.Events> records everything that happened, in order, as typed events: navigations starting and completing, redirects, view model creation, activation and deactivation, lifecycle method invocations, dialogs being shown, closed and dismissed, and busy state changes. Assert on it with LINQ:

```csharp
nav.Events.OfType<ViewModelLifecycleEvent>()
    .Select(e => e.Stage)
    .ShouldBe([ViewModelLifecycleStage.NavigatedTo, ViewModelLifecycleStage.RouteNavigated]);

nav.Events.OfType<DialogShownEvent>().ShouldHaveSingleItem();
```

Call <xref:Singulink.UI.Navigation.Testing.TestNavigator.ClearEvents> after setting up the starting state so assertions only cover the part of the test that matters.

## Dialogs

Dialogs shown by the code under test are real: they get a real dialog navigator, follow the same rules as in the application (only the top dialog can show children or close), and their tasks complete when they close.

### Scripting dialog outcomes

Register a script with <xref:Singulink.UI.Navigation.Testing.TestNavigator.OnDialogShown*> to respond to a dialog whenever it is shown. Scripts run after the dialog is showing, so the code that showed it is already awaiting the result:

```csharp
nav.OnDialogShown<ConfirmDeleteDialogModel>(dialog => dialog.Confirm());

await page.DeleteSelectedCommand.ExecuteAsync(null);
await nav.WaitUntilIdleAsync();

page.Items.ShouldBeEmpty();
```

Dialogs without a script stay open, so a test can drive them directly through <xref:Singulink.UI.Navigation.Testing.TestNavigator.TopDialog>:

```csharp
var deleteTask = page.DeleteSelectedAsync();

var dialog = nav.TopDialog.ShouldBeOfType<ConfirmDeleteDialogModel>();
dialog.Navigator.Close();

await deleteTask;
```

### Message dialogs

Message dialogs are answered with <xref:Singulink.UI.Navigation.Testing.TestNavigator.OnMessageDialog*>, which chooses the button index:

```csharp
nav.OnMessageDialog(dialog => dialog.ButtonLabels.ToList().IndexOf("Yes"));
```

An unscripted message dialog fails the test with the dialog's title and message, since it usually means the code took an unexpected path. Set <xref:Singulink.UI.Navigation.Testing.TestNavigator.AutoAcceptMessageDialogs> to `true` for tests that don't care, in which case the default button is chosen.

### Dismiss requests

<xref:Singulink.UI.Navigation.Testing.TestNavigator.RequestDismissTop> delivers the equivalent of the escape key or system back to the top dialog, so dismiss confirmations can be tested through the same path the application uses.

## Testing a Dialog View Model Directly

Dialog view models don't need routes. Show them through the test navigator and drive them:

```csharp
NavigationTestContext.Run(async () =>
{
    var nav = new TestNavigator(_ => { });

    var dialog = new EditItemDialogModel(item);
    var showTask = nav.ShowDialogAsync(dialog);   // dialog.Navigator is now live

    dialog.Name = "Renamed";
    await dialog.SaveCommand.ExecuteAsync(null);  // Calls this.Navigator.Close()

    (await showTask).ShouldBe(expectedResult);
});
```

Nested dialogs opened by the view model under test are real as well, and can be scripted or driven manually exactly like top-level dialogs.

## Waiting for Work to Finish

Commands often start work with <xref:Singulink.UI.Tasks.TaskRunner.RunAsBusyAndForget*> or return before all continuations have run. <xref:Singulink.UI.Navigation.Testing.TestNavigator.WaitUntilIdleAsync> waits until the navigator and every dialog are idle, so assertions can be made without arbitrary delays:

```csharp
page.RefreshCommand.Execute(null);
await nav.WaitUntilIdleAsync();

page.Items.Count.ShouldBe(3);
```

## Mock-Style Tests

For tests that prefer mocks over the real engine, <xref:Singulink.UI.Navigation.Testing.ViewModelTestSupport> associates a navigator or parameter with an already constructed view model:

```csharp
var navigator = Substitute.For<IDialogNavigator>();
var dialog = new EditItemDialogModel(item);
dialog.AttachNavigator(navigator);

dialog.SaveCommand.Execute(null);

navigator.Received().Close();
```

This suits dialog view models well, since <xref:Singulink.UI.Navigation.IDialogNavigator> is small. Routed view models are better served by <xref:Singulink.UI.Navigation.Testing.TestNavigator>: <xref:Singulink.UI.Navigation.INavigator> is large, and the application navigator makes the navigator and parameter available before the constructor runs, which an attach-after-construction approach cannot replicate.

</div>
