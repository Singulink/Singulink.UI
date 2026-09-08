# Singulink UI Toolkit

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)

**UI Toolkit** provides a variety of complimentary MVVM and XAML-based components that help streamline complex UI development scenarios. It is currently focused on WinUI and [Uno Platform](https://github.com/unoplatform/uno) applications but some components are UI framework-agnostic.

Details of each component are provided below:

| Library | Status | Package |
| --- | --- | --- |
| **Singulink.UI.Icons** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Icons.svg)](https://www.nuget.org/packages/Singulink.UI.Icons/) |
| **Singulink.UI.Icons.WinUI** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Icons.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Icons.WinUI/) |
| **Singulink.UI.Icons.Wpf** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Icons.Wpf.svg)](https://www.nuget.org/packages/Singulink.UI.Icons.Wpf/) |
| **Singulink.UI.Icons.Avalonia** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Icons.Avalonia.svg)](https://www.nuget.org/packages/Singulink.UI.Icons.Avalonia/) |
| **Singulink.UI.Icons.Maui** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Icons.Maui.svg)](https://www.nuget.org/packages/Singulink.UI.Icons.Maui/) |
| **Singulink.UI.Navigation** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation/) |
| **Singulink.UI.Navigation.WinUI** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation.WinUI/) |
| **Singulink.UI.Navigation.Testing** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.Testing.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation.Testing/) |
| **Singulink.UI.Tasks** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Tasks.svg)](https://www.nuget.org/packages/Singulink.UI.Tasks/) |
| **Singulink.UI.Xaml.WinUI** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Xaml.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Xaml.WinUI/) |

Libraries may be in the following states:
- Internal: Source code (and possibly a nuget package) is available but the library is intended for internal use at this time.
- Preview: Library is available for public preview but the APIs may not be fully documented and the API surface is subject to change without notice.
- Public: Library is intended for public use with a fully documented and stable API surface.

You are free to use any libraries or code in this repository that you find useful and feedback/contributions are welcome regardless of library state.

API documentation is available on the [project documentation site](https://www.singulink.com/Docs/Singulink.UI/index.html).

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional, and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

These packages are part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Components

### Singulink.UI.Icons

**Icon Pack Builder** (in the [IconPackBuilder](https://github.com/Singulink/Singulink.UI/tree/main/IconPackBuilder) folder) generates trimmed, strongly-typed font icon packs from Microsoft's Fluent UI System Icons (via the [Seagull Fluent Icons](https://github.com/davidxuang/FluentIcons) font). Pick the icons and variants your app uses and it exports a subset font containing only those glyphs plus a generated C# class with a member per icon, so icons are referenced by name and the font stays tiny. A CSS stylesheet and a JavaScript module (with TypeScript declarations) are exported alongside for web apps.

![Icon Pack Builder](Docs/images/icon-pack-builder.png)

**Key Features**:

✔️ Search by name or by Microsoft's metaphor keywords ("trash" finds Delete), with icon descriptions  
✔️ Regular, Filled, Color and Light variants, with per-icon export names  
✔️ Right-to-left glyphs for directional icons are exported automatically, and the framework packages display the correct glyph for the current flow direction  
✔️ Small JSON project file that is safe to commit and reloads when it changes on disk  
✔️ Scripted exports for CI with `iconpackbuilder export MyApp.Icons.ipproj`  

The **Singulink.UI.Icons** package provides the base types used by generated packs and is enough on its own for Windows Forms, Blazor or any other framework that renders text. **Singulink.UI.Icons.WinUI** (WinUI 3 and Uno Platform), **Singulink.UI.Icons.Wpf**, **Singulink.UI.Icons.Avalonia** and **Singulink.UI.Icons.Maui** add flow direction aware font icon elements for each framework.

**[Try it in your browser](https://iconpackbuilder.singulink.com/)** with nothing to install, or install the builder as a .NET global tool with `dotnet tool install -g Singulink.IconPackBuilder` and run `iconpackbuilder`. See the [Icon Guides](https://www.singulink.com/Docs/Singulink.UI/articles/icon-guides/getting-started.html) on the documentation site for a walkthrough of the builder and per-framework usage.

**Supported Platforms**: .NET 8.0+ for the base package and WPF/Avalonia packages; .NET 10.0+ for WinUI (WinAppSDK 1.7+), Uno Platform 6.5+ and .NET MAUI

### Singulink.UI.Navigation

Designed for handling MVVM-based applications with complex deep-linked navigation, with a strong emphasis on maintainability, separation of concerns and full testability of view models. We are confident that you will not want to use anything else once you try it out! 

**Key Features**:

✔️ First-class asynchronous navigation with automatic busy-state management  
✔️ "Strongly-typed everything" so the compiler can catch mistakes early and validate routes, parameters and navigations - no magic strings!  
✔️ "Zero code-behind" so you never need to handle events or override methods in pages or dialogs  
✔️ Simple navigation configuration, all business logic contained within view models  
✔️ Comprehensive deep-linking support with automatic route parameter parsing  
✔️ Compatible with all MVVM frameworks  
✔️ Single window or multi-window apps, nested child navigation views  
✔️ Intuitive, straightforward and foolproof content dialogs / message dialogs / nested dialogs  
✔️ Built-in comprehensive hierarchy aware DI, easy to use with or without an additional DI container for root services  
✔️ Full integration with **Singulink.UI.Tasks** ([see below](#singulinkuitasks)) for simple and easy management of busy-state while long running operations are executing on pages or dialogs  

The base library is not tied to any particular UI framework but currently only WinUI/Uno-specific implementations of the base library types are provided via the `Singulink.UI.Navigation.WinUI` package to do the actual navigation and routing in the UI app layer. We may add support for additional UI frameworks in the future.

See the [Navigation Guides](https://www.singulink.com/Docs/Singulink.UI/articles/navigation-guides/getting-started.html) on the documentation site for a tour of the API and common usage patterns. You can also explore the [Playground](https://github.com/Singulink/Singulink.UI/tree/main/Playground) and [Icon Pack Builder](https://github.com/Singulink/Singulink.UI/tree/main/IconPackBuilder) projects for real-world examples.

**Supported Platforms**: .NET 10.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.5+

### Singulink.UI.Xaml.WinUI

Contains useful XAML extensions (behaviors, converters, static convert methods for use with `x:Bind`) for WinUI and Uno-based applications.

Here is a small sampling of the huge collection of static convert methods available:

```cs
xmlns:c="using:Singulink.UI.Xaml.Converters"

IsEnabled="{x:Bind c:If.Zero(Model.Items.Count)}"
IsEnabled="{x:Bind c:If.NotZero(Model.Items.Count)}"
IsEnabled="{x:Bind c:If.Null(Model.Item)}"
IsEnabled="{x:Bind c:If.NotNullOrWhiteSpace(Model.Name)}"
IsEnabled="{x:Bind c:If.NotDefault(Model.SomeEnumValue)}"

Visibility="{x:Bind c:Visible.IfStringEqualsAny(Model.EnumValue, 'EnumName1', 'EnumName2')}"
Visibility="{x:Bind c:Visible.IfFocused(SomeOtherControl.FocusState)}"
Visibility="{x:Bind c:Visible.IfFalse(Model.Hide)}"

Opacity="{x:Bind c:Opaque.IfTrue(Model.ShowValue)}"

Uri="{x:Bind c:Uri.Email(Model.EmailString)}"
Uri="{x:Bind c:Uri.Phone(Model.PhoneString)}"
Uri="{x:Bind c:Uri.Website(Model.WebsiteString)}"
```

**Supported Platforms**: .NET 10.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.5+

### Singulink.UI.Tasks

Provides a DI-friendly and UI framework-agnostic task runner/dispatcher with integrated support for managing UI busy-state while tasks are running. Supports running "fire-and-forget" tasks that can be tracked and fully tested with exceptions being propagated back to the UI thread, avoiding frowned upon `async void` methods for things like event handlers.

**TaskRunner** is fully integrated with **Singulink.UI.Navigation**. [See above](#singulinkuinavigation) for documentation on how it should be used in that scenario.

Here is an example of usage when `TaskRunner` is not used with the navigation framework:

```cs
public class App
{
  public static ITaskRunner TaskRunner { get; private set; }

  public void OnAppStart()
  {
    // Assign to a singleton you can pass around, or register with your DI container here

    TaskRunner = new TaskRunner(
      busy => YourRootControl.IsEnabled = !busy);
  }
}

public class YourViewModel(ITaskRunner taskRunner)
{
  public ObservableCollection<Item> Items { get; } = [];

  // Fire and forget example:

  public void OnNavigatedTo()
  {
    // YourRootControl.IsEnabled will be false while this runs

    taskRunner.RunAsBusyAndForget(async () =>
    {
      var items = await LoadItemsAsync();

      foreach (var item in items)
      {
        Items.Add(item);
      }
    });
  }

  // Command that runs a task which should indicate busy state:

  [RelayCommand]
  public async Task SaveAsync()
  {
    // YourRootControl.IsEnabled will be false while this runs

    await taskRunner.RunAsBusyAsync(async () =>
    {
      await ApiClient.SaveAsync(Data);
    });
  }
}
```

Our philosophy is that testing view models without a proper synchronization context that simulates a main UI thread is asking for trouble, so `TaskRunner` requires one. The [AsyncEx.Context](https://github.com/StephenCleary/AsyncEx) library has a perfect `AsyncContext` class that can be used for this purpose. Your test would then look something like this:

```cs
[TestClass]
public class YourViewModelTests
{
  [TestMethod]
  public void TestLoadsItemsAsync()
  {
    AsyncContext.Run(async () =>
    {
      var taskRunner = new TaskRunner();

      var vm = new YourViewModel(taskRunner);
      vm.OnNavigatedTo();

      // Wait for all busy tasks to complete
      await taskRunner.WaitForIdleAsync(waitForNonBusyTasks: false);

      Assert.AreEqual(3, vm.Items.Count);
    });
  }
}
```

**Supported Platforms**: .NET 8.0+, any UI framework (i.e. UWP/WinUI, Uno Platform, Avalonia, WPF, etc)

## Further Reading

Please head over to the [project documentation site](https://www.singulink.com/Docs/Singulink.UI/index.html) to view articles, examples and the fully documented API.

## Shoutouts 🎉

**Singulink.UI.Tasks** was inspired by [AmbientTasks](https://github.com/Techsola/AmbientTasks) (thanks [@jnm2](https://github.com/jnm2)!).
