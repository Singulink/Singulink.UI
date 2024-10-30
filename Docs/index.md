<div class="article">

# Singulink UI Toolkit

## Overview

**UI Toolkit** provides components that are generally useful for UI applications with a strong emphasis on testability of view models. It is currently focused on WinUI and Uno-based applications but some components are UI framework-agnostic. Details of each component are provided below.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Components

### Singulink.UI.Navigation

Strongly-typed AOT-friendly navigation framework with comprehensive deep-linking support. The base library is UI framework-agnostic and can be referenced from framework-agnostic view model projects, but currently only a WinUI/Uno-specific `INavigator` implementation is provided via the `Singulink.UI.Navigation.WinUI` package to do the actual navigation and routing in the UI app layer. Can be extended to support other UI frameworks, and contributions are welcome.

There is an additional 'Singulink.UI.Navigation.MvvmToolkit' package that provides base implementations of routed view models that inherit from the MVVM Community Toolkit's `ObservableObject` type.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK), Uno Platform 5.2+

### Singulink.UI.Tasks

Provides a DI-friendly and UI framework-agnostic task runner/dispatcher with integrated support for managing UI busy-state while tasks are running. Supports running "fire-and-forget" tasks that can be tracked and fully tested. Inspired by [AmbientTasks](https://github.com/Techsola/AmbientTasks) (thanks [@jnm2](https://github.com/jnm2))

**Supported Platforms**: .NET 8.0+, any UI framework (i.e. UWP/WinUI, Uno Platform, Avalonia, WPF, etc)

### Singulink.UI.Xaml.WinUI

Contains useful XAML extensions (behaviors, converters, static convert methods for use with `x:Bind`) for WinUI and Uno-based applications.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK), Uno Platform 5.2+


## Information and Links

Here are some additonal links to get you started:

- [API Documentation](api/index.md) - Browse the fully documented API here.
- [Chat on Discord](https://discord.gg/EkQhJFsBu6) - Have questions or want to discuss the library? This is the place for all Singulink project discussions.
- [Github Repo](https://github.com/Singulink/Singulink.UI) - File issues, contribute pull requests or check out the code for yourself!

</div>
