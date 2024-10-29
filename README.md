# Singulink UI Toolkit

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)

**UI Toolkit** provides components that are generally useful for UI applications with a strong emphasis on testability of view models. It is currently focused on WinUI and Uno-based applications but some components are UI framework-agnostic. Details of each component are provided below.

| Library | Status | Package |
| --- | --- | --- |
| **Singulink.UI.Navigation** | Preview | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation/) |
| **Singulink.UI.Navigation.MvvmToolkit** | Preview | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.MvvmToolkit.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation.MvvmToolkit/) |
| **Singulink.UI.Navigation.WinUI** | Preview | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Navigation.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Navigation.WinUI/) |
| **Singulink.UI.Tasks** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Tasks.svg)](https://www.nuget.org/packages/Singulink.UI.Tasks/) |
| **Singulink.UI.Xaml.WinUI** | Public | [![View nuget package](https://img.shields.io/nuget/v/Singulink.UI.Xaml.WinUI.svg)](https://www.nuget.org/packages/Singulink.UI.Xaml.WinUI/) |

Libraries may be in any of the following states:
- Internal: Source code (and possibly a nuget package) is available to the public but the library is intended to be used internally until further development.
- Preview: The library is available for public preview but the APIs may not be fully documented and the API surface is subject to change without notice.
- Public: The library is intended for public use with a fully documented and stable API surface.

You are free to use any libraries or code in this repository that you find useful and feedback/contributions are welcome regardless of library state.

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional, and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Components

### Singulink.UI.Navigation

Strongly-typed AOT-friendly navigation framework with comprehensive deep-linking support. The base library is UI framework-agnostic and can be referenced from framework-agnostic view model projects, but currently only a WinUI/Uno-specific `INavigator` implementation is provided via the `Singulink.UI.Navigation.WinUI` package to do the actual navigation and routing in the UI app layer. Can be extended to support other UI frameworks, and contributions are welcome.

There is an additional 'Singulink.UI.Navigation.MvvmToolkit' package that provides base implementations of routed view models that inherit from the MVVM Community Toolkit's `ObservableObject` type.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK), Uno Platform 5.2+

### Singulink.UI.Tasks

Provides a DI-friendly and UI framework-agnostic task runner/dispatcher with integrated support for managing UI busy-state while tasks are running. Supports running "fire-and-forget" tasks that can be tracked and fully tested. Inspired by [AmbientTasks](https://github.com/Techsola/AmbientTasks) (thanks [@jnm2](https://github.com/jnm2))

Example fire-and-forget usage:

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

  public void OnNavigatedTo()
  {
    // YourRootControl.IsEnabled will be false while this runs

    taskRunner.RunAndForget(setBusy: true, async () =>
    {
      var items = await LoadItemsAsync();

      foreach (var item in items)
      {
        Items.Add(item);
      }
    });
  }
}
```

Our philosophy is that testing view models without a proper synchronization context is asking for trouble, so `TaskRunner` requires one. The [AsyncEx.Context](https://github.com/StephenCleary/AsyncEx) library has a perfect `AsyncContext` class that can be used for this purpose. Your test would then look something like this:

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
      await taskRunner.WaitForIdleAsync();

      Assert.AreEqual(3, vm.Items.Count);
    });
  }
}
```

**Supported Platforms**: .NET 8.0+, any UI framework (i.e. UWP/WinUI, Uno Platform, Avalonia, WPF, etc)

### Singulink.UI.Xaml.WinUI

Contains useful XAML extensions (behaviors, converters, static convert methods for use with `x:Bind`) for WinUI and Uno-based applications.

Here is a small sampling of the massive collection of static convert methods available:

```cs
xmlns:suxc="using:Singulink.UI.Xaml.Converters"

IsEnabled="{x:Bind suxc:If.Zero(Model.Items.Count)}"
IsEnabled="{x:Bind suxc:If.NotZero(Model.Items.Count)}"
IsEnabled="{x:Bind suxc:If.Null(Model.Item)}"
IsEnabled="{x:Bind suxc:If.NotNullOrWhiteSpace(Model.Name)}"
IsEnabled="{x:Bind suxc:If.NotDefault(Model.SomeEnumValue)}"

Visibility="{x:Bind suxc:Visible.IfEqualsAnyString(Model.EnumValue, 'EnumName1', 'EnumName2')}"
Visibility="{x:Bind suxc:Visible.IfFocused(SomeOtherControl.FocusState)}"
Visibility="{x:Bind suxc:Visible.IfFalse(Model.Hide)}"

Opacity="{x:Bind suxc:Opaque.IfTrue(Model.ShowValue)}"}

Uri="{x:Bind suxc:Uri.Email(Model.EmailString)}"
Uri="{x:Bind suxc:Uri.Phone(Model.PhoneString)}"
Uri="{x:Bind suxc:Uri.Website(Model.WebsiteString)}"
```

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK), Uno Platform 5.2+

## Further Reading

You can view API documentation on the [project documentation site](https://www.singulink.com/Docs/Singulink.UI/index.html).
