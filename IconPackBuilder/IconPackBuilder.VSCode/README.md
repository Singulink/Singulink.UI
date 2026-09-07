# Singulink Icon Pack Builder

Build trimmed, strongly typed font icon packs for your .NET apps without leaving VS Code.

Icon Pack Builder takes Microsoft's [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) (via the single-font [Seagull Fluent Icons](https://github.com/davidxuang/FluentIcons) build), lets you pick the icons and variants your app uses, and exports:

- a **subset font** (`.otf`) containing only the glyphs you selected, typically a few tens of kilobytes instead of over a megabyte, and
- a **generated C# class** with a static member per icon, so icons are referenced by name and the compiler catches typos, and
- a **CSS stylesheet** and a **JavaScript module** (with TypeScript declarations) for using the same pack in web apps.

Right-to-left versions of directional icons are exported automatically, and the [Singulink.UI.Icons](https://www.nuget.org/packages/Singulink.UI.Icons) packages display the correct glyph for the current flow direction in WinUI / Uno Platform, WPF, Avalonia and .NET MAUI.

## Getting started

1. Run **Icon Pack Builder: New Icon Pack Project...** from the command palette, the Explorer folder context menu or the **New File...** picker. The project name must be in `Namespace.Class` form (for example `MyApp.FontIcons`); it becomes the namespace and class name of the generated code.
2. The project opens in the Icon Pack Builder editor. Search by name or by Microsoft's metaphor keywords ("trash" finds Delete, "house" finds Home), tick the variants you need, and rename exports where an app-specific name reads better.
3. Every change is written to the `.ipproj` file straight away, so the tab shows the usual dirty marker, Ctrl+S saves and Ctrl+Z undoes. The file is small JSON that is safe to commit.
4. **File → Export** in the editor writes a `<ProjectName>_Export` folder next to the project containing `<ProjectName>.otf`, `<Class>.cs`, `<Class>.css` and `<Class>.js` (with `<Class>.d.ts`). Add the font and the files your app needs, and reference the appropriate Singulink.UI.Icons package for .NET. **File → Export Formats** turns individual outputs off; the selection is saved in the project.

**Icon Pack Builder: Open as JSON** shows the raw project file; `.ipproj` files have JSON highlighting and schema validation, and edits made there are picked up by the visual editor.

See the [Icon Guides](https://www.singulink.com/Docs/Singulink.UI/articles/icon-guides/getting-started.html) for per-framework usage, right-to-left support and the desktop version of the builder (`dotnet tool install -g Singulink.UI.IconPackBuilder`).

## Requirements

No .NET SDK or Python is required: the editor runs as WebAssembly inside VS Code and the font subsetter is bundled. Platform-specific packages are published for Windows (x64), macOS (Intel and Apple Silicon) and Linux (x64 and Arm64, glibc 2.28 or later). On other platforms exporting falls back to a `pyftsubset` on the PATH or a Python installation with [fonttools](https://pypi.org/project/fonttools/) installed.

Exporting requires the project to be on a local file system (it is not available in virtual workspaces).

## Notes

- The editor boots a .NET runtime on first open, which takes a few seconds; it stays loaded while the tab is open.
- Console output from the editor is available in the **Icon Pack Builder** output channel for troubleshooting.

## Source

The extension, the desktop app and the icon packages live in the [Singulink.UI](https://github.com/Singulink/Singulink.UI) repository. Issues and feature requests are welcome there.
