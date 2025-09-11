# Singulink UI Toolkit

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)

**UI Toolkit** provides a variety of complimentary MVVM and XAML-based components that help streamline complex UI development scenarios. It is currently focused on WinUI and Uno-based applications but some components are UI framework-agnostic.

Details of each component are provided below:

| Library | Status | Package |
| --- | --- | --- |
| **Singulink.UI.Navigation** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation/) |
| **Singulink.UI.Navigation.WinUI** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation.WinUI/) |
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
✔️ Easy to use with or without a DI container  
✔️ Full integration with **Singulink.UI.Tasks** ([see below](#singulinkuitasks)) for simple and easy management of busy-state while long running operations are executing on pages or dialogs  

The base library is not tied to any particular UI framework and can be referenced from framework-agnostic view model projects, but currently only WinUI/Uno-specific implementations of the base library types are provided via the `Singulink.UI.Navigation.WinUI` package to do the actual navigation and routing in the UI app layer. We plan to add more UI framework implementations soon (probably WPF and Avalonia initially).

Stay tuned, additional documentation and examples are also coming soon! You are welcome to have a look at the [Playground](https://github.com/Singulink/Singulink.UI/tree/main/Playground) project to get an idea of how it works for now.

Some key parts of the Playground to check out are:

- [`Routes.cs`](https://github.com/Singulink/Singulink.UI/blob/main/Playground/Playground.ViewModels/Routes.cs): Contains the strongly-typed route and parameter definitions. This should be together with your view models so they can make compiler-checked navigation calls.
- [`AppWindow.cs`](https://github.com/Singulink/Singulink.UI/blob/main/Playground/Playground/AppWindow.cs): The main app window where "view model to view" mappings are defined and the navigator is configured. Back button handling is also setup here.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.0+

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

Opacity="{x:Bind c:Opaque.IfTrue(Model.ShowValue)}"}

Uri="{x:Bind c:Uri.Email(Model.EmailString)}"
Uri="{x:Bind c:Uri.Phone(Model.PhoneString)}"
Uri="{x:Bind c:Uri.Website(Model.WebsiteString)}"
```

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.0+

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
      await ApiClient.SaveAsync(Data));
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

You can view API documentation on the [project documentation site](https://www.singulink.com/Docs/Singulink.UI/index.html).

## Shoutouts 🎉

**Singulink.UI.Tasks** was inspired by [AmbientTasks](https://github.com/Techsola/AmbientTasks) (thanks [@jnm2](https://github.com/jnm2)!).
