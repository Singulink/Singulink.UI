<div class="article">

# Singulink UI Toolkit

## Overview

**UI Toolkit** provides a variety of complimentary MVVM and XAML-based components that help streamline complex UI development scenarios. It is currently focused on WinUI and Uno-based applications but some components are UI framework-agnostic. Details of each component are provided below.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Components

### Singulink.UI.Navigation

Designed for handling MVVM-based applications with complex deep-linked navigation, with a strong emphasis on maintainability, separation of concerns and full testability of view models. The base library is not tied to any particular UI framework and can be referenced from framework-agnostic view model projects, but currently only WinUI/Uno-specific implementations of the base library types are provided via the `Singulink.UI.Navigation.WinUI` package to do the actual navigation and routing in the UI app layer.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.0+

### Singulink.UI.Xaml.WinUI

Contains useful XAML extensions (behaviors, converters, static convert methods for use with `x:Bind`) for WinUI and Uno-based applications.

**Supported Platforms**: .NET 8.0+, WinUI (WinAppSDK 1.7+), Uno Platform 6.0+

### Singulink.UI.Tasks

Provides a DI-friendly and UI framework-agnostic task runner/dispatcher with integrated support for managing UI busy-state while tasks are running. Supports running "fire-and-forget" tasks that can be tracked and fully tested with exceptions being propagated back to the UI thread, avoiding frowned upon `async void` methods for things like event handlers.

**Supported Platforms**: .NET 8.0+, any UI framework (i.e. UWP/WinUI, Uno Platform, Avalonia, WPF, etc)


## Information and Links

Here are some additonal links to get you started:

- [API Documentation](api/index.md) - Browse the fully documented API here.
- [Chat on Discord](https://discord.gg/EkQhJFsBu6) - Have questions or want to discuss the library? This is the place for all Singulink project discussions.
- [Github Repo](https://github.com/Singulink/Singulink.UI) - File issues, contribute pull requests or check out the code for yourself!

</div>
