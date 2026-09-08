# Singulink Icon Pack Builder

Icon Pack Builder creates trimmed font-based icon packs from the [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons): pick the icons your app needs, export a subset font plus a strongly typed C# class, and display the icons with the [Singulink.UI.Icons](https://www.nuget.org/packages/Singulink.UI.Icons) packages for WinUI / Uno Platform, WPF, Avalonia, .NET MAUI or any other framework that renders text. A CSS stylesheet and a JavaScript module (with TypeScript declarations) are exported alongside for web apps. Right-to-left variants of directional icons are preserved so mirrored layouts get the correct glyph automatically.

## Install

```
dotnet tool install -g Singulink.IconPackBuilder
```

The tool is framework-dependent and runs on Windows (x64 / Arm64), macOS (x64 / Apple Silicon) and Linux (x64 / Arm64) with the .NET 10 runtime or later.

## Use

```
iconpackbuilder                     # opens the start screen
iconpackbuilder MyApp.Icons.ipproj  # opens a project directly
iconpackbuilder --wait ...          # keeps the console attached until the app exits
iconpackbuilder export MyApp.Icons.ipproj   # writes the export folder without opening the editor (scripts / CI)
```

The `export` command exits with a non-zero code if the project cannot be exported in full, for example when it references an icon that is missing from the builder's icon source.

To open `.ipproj` files with a double-click, register a per-user file association (Windows and Linux; no administrator rights needed):

```
iconpackbuilder --register-file-association
iconpackbuilder --unregister-file-association
```

Documentation: https://www.singulink.com/Docs/Singulink.UI/articles/icon-guides/getting-started.html
