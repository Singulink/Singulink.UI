# Icon Pack Builder

Utility app for building trimmed font-based icon packs from the Seagull Fluent Icons font.

## Projects

- **IconPackBuilder** - the Uno Platform app (WinAppSDK and Skia desktop heads). Requires MSBuild to build because of the WinAppSDK head.
- **IconPackBuilder.Core** - icon source abstraction, the Seagull icons source and exporter/subsetter service interfaces.
- **IconPackBuilder.ViewModels** - view models.
- **IconPackBuilder.Data** - project file (`.ipproj`) model.
- **IconPackBuilder.SeagullAssetsGenerator** - maintenance tool that regenerates the Seagull assets (see below).
- **Tests/IconPackBuilder.Tests** - tests for the icon source, exporter, recent projects store and view models (run with `dotnet test`).

## Opening a project from the command line

Pass a project file to open it directly in the editor: `IconPackBuilder.exe MyApp.FontIcons.ipproj`. The same works for the Skia desktop head.

## Seagull icon assets

The app does not reference the FluentIcons packages at runtime. Instead, `IconPackBuilder/Assets/Seagull` contains two generated files that are
checked in:

- `SeagullFluentIcons.otf` - the font, taken from the `FluentIcons.WinUI` NuGet package.
- `SeagullFluentIcons.json` - every `Symbol` in the matching `FluentIcons.Common` package with its code points per variant, RTL code points where
  the font has a distinct RTL glyph, and the description, metaphors (search keywords) and direction type from the Microsoft Fluent UI System Icons
  repository at the commit that the FluentIcons release was built from.

To update to a newer FluentIcons release:

1. Set `FluentIconsCommonVersion`, `FluentIconsWinUIVersion` and `FluentIconsTag` in `IconPackBuilder.SeagullAssetsGenerator.csproj`.
2. Run `dotnet run --project IconPackBuilder/IconPackBuilder.SeagullAssetsGenerator` from the repository root (needs network access and `git`).
3. Review the summary it prints (symbols without upstream metadata are listed) and commit the regenerated assets.

Symbol enum names and values are stable across FluentIcons releases, so existing `.ipproj` projects keep exporting the same code points after an
update; new symbols simply become available.

## Font subsetting (pyftsubset)

Exports subset the icon font with fonttools' `pyftsubset`, run as a single-file PyInstaller binary from `Tools/<runtime identifier>/` in the app
directory. `PyFtSubsetter` resolves the tool in this order:

1. The bundled binary for the current platform (`Tools/win-x64/pyftsubset.exe`, `Tools/osx-arm64/pyftsubset`, ...).
2. A `pyftsubset` found on the `PATH`.
3. A Python installation with fonttools installed (`python -m fontTools.subset`; install with `pip install fonttools`).

Only the Windows x64 binary is committed. Binaries for the other platforms (linux-x64, linux-arm64, osx-x64, osx-arm64) are built by the
**Build pyftsubset binaries** GitHub Actions workflow (`.github/workflows/pyftsubset.yml`, run it manually) and published as assets of the
`pyftsubset-v<fonttools version>` release. `Tools/PyFtSubset.targets` downloads the asset for the build platform (or the `RuntimeIdentifier` being
published) into `obj/` on first build and copies it to the output. After publishing binaries for a new fonttools version, update
`PyFtSubsetVersion` in that targets file.

Linux binaries are built in `manylinux_2_28` containers, so they run on any glibc 2.28 or later distribution regardless of distro family; musl-based
distributions such as Alpine fall back to the `PATH`/Python options above.
