# Changelog

## 1.0.0

Initial release.

- Opens `.ipproj` files in the Icon Pack Builder editor (the WebAssembly build of the desktop app) with VS Code owning the document: dirty state, save, undo/redo and external change reloads work like any text file.
- **Icon Pack Builder: New Icon Pack Project...** creates a project (also available from the Explorer folder context menu and the New File picker).
- **Export** writes the subset font, the generated C# class, a CSS stylesheet and a JavaScript module (with TypeScript declarations) next to the project using the bundled `pyftsubset`. **File → Export Formats** selects which outputs to write.
- **Icon Pack Builder: Open as JSON** switches to the text editor; `.ipproj` files get JSON highlighting and schema validation.
- Follows the VS Code color theme.
