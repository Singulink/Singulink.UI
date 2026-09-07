# Singulink Icon Pack Builder for VS Code: development notes

The user-facing description lives in `README.md` (it becomes the Marketplace listing). This file covers building, packaging and the
host/app bridge.

VS Code extension that opens `.ipproj` files (Icon Pack Builder projects) in a custom editor hosting the Uno Platform
WebAssembly build of Icon Pack Builder, and exports trimmed Seagull Fluent Icons font packs using a bundled `pyftsubset`.

The project file is plain JSON, so VS Code remains the owner of the document: dirty state, save, undo/redo, source
control diffs and external change reloads all work as they do for any text file. The app inside the webview receives the
document text and sends back replacement text when the project changes. "Icon Pack Builder: Open as JSON" (command
palette or the editor title button) reopens the file in the regular text editor.

## Layout

| Path | Purpose |
| --- | --- |
| `src/extension.ts` | Activation; registers the custom editor and the "Open as JSON" command. |
| `src/ipprojEditorProvider.ts` | `CustomTextEditorProvider`, webview message handling, export implementation. |
| `src/webviewHtml.ts` | Generates the webview HTML (CSP, `<base>`, bridge script, Uno bootstrapper tags). |
| `src/pyftsubset.ts` | Locates and runs `pyftsubset` (bundled binary, PATH, or Python fallbacks). |
| `scripts/package.mjs` | Builds one VSIX per platform target with the matching `pyftsubset` binary. |
| `media/app/` | Published WebAssembly output (build artifact, not committed). |
| `bin/` | Platform `pyftsubset` binary (copied in at package time, not committed). |

## Building

Prerequisites: Node.js 20+, the .NET 10 SDK with the `wasm-tools` workload, and the `pyftsubset` binaries (see the
Icon Pack Builder README for how they are produced; they are release assets of the `pyftsubset-v<version>` GitHub
release, plus the committed `Tools/win-x64/pyftsubset.exe`).

1. Publish the WebAssembly head from the repository root:

   ```
   dotnet publish IconPackBuilder/IconPackBuilder/IconPackBuilder.csproj -c Release -f net10.0-browserwasm
   ```

   The static site ends up in `IconPackBuilder/IconPackBuilder/bin/Release/net10.0-browserwasm/publish/wwwroot`
   (an `index.html`, `_framework/` with `dotnet.js`, `dotnet.wasm`, assemblies and `.dat` files, and `package_*/`
   asset folders).

2. The published site keeps the bootstrapper (`require.js`, `uno-bootstrap.js`, its CSS) in a content-hashed
   `package_<hash>/` folder next to `_framework/`. The extension discovers that folder at runtime, so nothing needs
   updating after a republish. The pre-compressed `.br`/`.gz` copies Uno emits for static hosting are skipped when the
   app is copied into `media/app/` (they roughly triple the size and the webview never uses them).

   The package script also points the loader's splash image at the app icon (which keeps transparent corners) and the
   webview page colors the loader with the VS Code editor background, so the splash matches any theme.

   The app project sets `WasmShellWebAppBasePath` to `./` for the WebAssembly head so `uno-config.js` uses relative
   paths; root-absolute paths (`/package_<hash>/...`) would resolve against the `vscode-webview://` origin and fail. The
   package script additionally rewrites any remaining absolute paths, disables the PWA service worker (a webview cannot
   register one) and sets `uno_shell_mode` to `BrowserEmbedded`. The last one matters: Uno's bootstrapper treats any
   user agent containing "Electron" as a Node host and loads its dependencies with synchronous CommonJS `require()`,
   which does not exist in a webview; `BrowserEmbedded` is the only mode that skips that detection.

3. Install dependencies and compile:

   ```
   cd IconPackBuilder/IconPackBuilder.VSCode
   npm install
   npm run compile
   ```

4. Package. Put the binaries in one folder named by .NET runtime identifier (`pyftsubset-win-x64.exe`,
   `pyftsubset-linux-x64`, `pyftsubset-linux-arm64`, `pyftsubset-osx-x64`, `pyftsubset-osx-arm64`) and run:

   ```
   npm run package -- --bin-dir <binaries> --app-dir ../IconPackBuilder/bin/Release/net10.0-browserwasm/publish/wwwroot
   ```

   Use `--targets win32-x64,darwin-arm64` to build a subset. One `.vsix` per target is written to `dist/`. Each package
   is platform-specific because it contains a native `pyftsubset` binary; publish all of them to the Marketplace under
   the same version and VS Code picks the right one.

For development, press F5 in this folder (the launch configuration compiles first). Copy the published `wwwroot` into
`media/app/` manually (or run the package script once with `--app-dir`) so the webview has something to load. Without
a `bin/` binary the extension falls back to `pyftsubset` on the PATH, then `python3`/`python -m fontTools.subset`.

## How the bridge works

`src/webviewHtml.ts` generates the webview page. It cannot reuse the published `index.html` because the webview needs a
nonce-based Content Security Policy, a `<base href>` pointing at the extension's `media/app/` resource URI so the .NET
runtime loader can fetch `_framework/...` relatively, and the host bridge must be installed before the app starts.

The page runs an inline classic script that installs `window.ipbHost` and posts `{ type: 'ready' }` to the extension
host, then loads `require.js` and the `uno-bootstrap.js` module the same way Uno's own `index.html` does. The `acquireVsCodeApi()` handle is captured inside
the bridge closure and never exposed, so the app can only talk to the host through `ipbHost`.

### `window.ipbHost` (what the .NET side calls via `[JSImport]`)

| Function | Description |
| --- | --- |
| `whenReady(): Promise<void>` | Resolves after the host delivers the `init` message. Await this before reading the document; the runtime usually boots long after `init` arrives, but do not rely on it. |
| `getDocumentText(): string` | Latest document text (from `init`, `documentChanged`, or the last `setDocumentText`). |
| `setDocumentText(text: string): void` | Replaces the whole document. Posts `{ type: 'edit', text }`; the host applies a `WorkspaceEdit` so VS Code tracks dirty state and undo. |
| `onDocumentChanged(callback: (text: string) => void): void` | Registers a callback for changes made outside the app (text editor, undo, external file change, git checkout). Not invoked for the app's own edits. |
| `exportProject(requestJson: string): Promise<string>` | Runs an export (see below). Resolves with `{ "ok": boolean, "exportDir": string, "error": string \| null }` as a JSON string. |
| `showMessage(message: string, title: string, buttonsJson: string): Promise<number>` | Shows a modal VS Code message with the given button labels (`buttonsJson` is a JSON string array). Resolves with the chosen button index or -1 if dismissed. Pass an empty title to show `message` as the heading. |
| `getDocumentFileName(): string` | File name of the open document, e.g. `MyApp.FontIcons.ipproj`. |
| `getThemeKind(): string` | VS Code color theme kind: `light`, `dark`, `highContrast` or `highContrastLight`. |
| `onThemeChanged(callback: (kind: string) => void): void` | Registers a callback for theme changes. |
| `getDocumentDir(): string` | Directory of the document on disk, or `""` if the document is not a file. |
| `hideLoadingIndicator(): void` | Removes the host's "Loading..." overlay. It is also removed automatically once Uno attaches its root element to `#uno-body`. |

Example `[JSImport]` declarations:

```csharp
internal static partial class IpbHost
{
    [JSImport("globalThis.ipbHost.whenReady")]
    internal static partial Task WhenReady();

    [JSImport("globalThis.ipbHost.getDocumentText")]
    internal static partial string GetDocumentText();

    [JSImport("globalThis.ipbHost.setDocumentText")]
    internal static partial void SetDocumentText(string text);

    [JSImport("globalThis.ipbHost.onDocumentChanged")]
    internal static partial void OnDocumentChanged([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

    [JSImport("globalThis.ipbHost.exportProject")]
    internal static partial Task<string> ExportProject(string requestJson);

    [JSImport("globalThis.ipbHost.showMessage")]
    internal static partial Task<int> ShowMessage(string message, string title, string buttonsJson);

    [JSImport("globalThis.ipbHost.getDocumentFileName")]
    internal static partial string GetDocumentFileName();

    [JSImport("globalThis.ipbHost.getDocumentDir")]
    internal static partial string GetDocumentDir();
}
```

### Export request

`exportProject` takes a JSON string with this shape (camelCase property names):

```json
{
    "projectName": "MyApp.FontIcons",
    "exportDirName": "MyApp.FontIcons",
    "fontFileName": "MyApp.FontIcons.otf",
    "fontBase64": "<source SeagullFluentIcons.otf, base64>",
    "codePoints": [ 59648, 59649 ],
    "files": [
        { "name": "MyApp.FontIcons.cs", "contentBase64": "<base64>" }
    ]
}
```

The host:

1. Validates that `exportDirName`, `fontFileName` and every `files[].name` are plain file names (no path separators or
   `..`), so the export cannot write outside `<documentDir>/<exportDirName>/`.
2. Writes the source font and a unicodes file (one `U+XXXX` per line) to a temp directory.
3. Deletes `<documentDir>/<exportDirName>/` (stale output) and recreates it, then writes `files`.
4. Runs `pyftsubset <tempFont> --unicodes-file=<tempUnicodes> --output-file=<exportDir>/<fontFileName>`, the same
   invocation the desktop app uses.
5. Replies with `exportResult` and shows an information message with a "Reveal in Explorer" action, or an error message
   with the tool's stderr if subsetting failed.

The document must be saved on disk (`getDocumentDir()` non-empty); exporting an untitled document fails with an error.

### Message protocol (for reference)

Webview to host: `ready`, `edit { text }`, `export { requestId, request }`,
`showMessage { requestId, message, title?, buttons }`.

Host to webview: `init { text, fileName, dir, themeKind }`, `documentChanged { text }`, `themeChanged { themeKind }`,
`exportResult { requestId, ok, exportDir, error? }`, `showMessageResult { requestId, index }`.

The host tracks the text the webview last saw and only pushes `documentChanged` when the document differs from it, which
is what prevents the app's own `setDocumentText` calls from echoing back as change notifications.

## Content Security Policy notes

The webview CSP is:

```
default-src 'none';
img-src ${cspSource} blob: data:;
style-src ${cspSource} 'unsafe-inline';
font-src ${cspSource} data:;
script-src 'nonce-${nonce}' ${cspSource} 'wasm-unsafe-eval' 'unsafe-eval';
connect-src ${cspSource} data: blob:;
worker-src ${cspSource} blob:;
```

- `'wasm-unsafe-eval'` is required to compile and instantiate `dotnet.wasm`. Without it the runtime fails with a
  CSP error at `WebAssembly.instantiate`.
- `'unsafe-eval'` is required because Uno's WebAssembly runtime evaluates JavaScript strings for its interop layer.
  Without it the first UI dispatcher call fails with an `EvalError` and the app dies with a `NullReferenceException`
  in the renderer before anything is drawn.
- `${cspSource}` in `script-src` lets `uno-bootstrap.js`/`dotnet.js` import their sibling modules from the extension
  resource origin; the nonce covers the inline bridge and the module loader tag.
- `connect-src` covers `fetch` of `_framework/*` (assemblies, ICU data, config) and blob/data URLs the runtime creates.
- `'unsafe-inline'` for styles is needed because Uno/Skia injects inline `style` attributes.
- The .NET runtime must be published single-threaded. Browsers refuse to start cross-origin workers and the app resources
  come from the `vscode-resource` origin, so `WasmEnableThreads` would not work here.
- The app has no network access to anything other than the extension's own resources. Everything it needs (font,
  symbol metadata) must be part of the published output.

## Known limitations

- **Cold start.** The .NET runtime plus the Uno app takes a few seconds to download from disk, compile and start on
  first open. The webview is retained while hidden (`retainContextWhenHidden`) so switching tabs does not restart it,
  but every newly opened `.ipproj` boots a fresh runtime.
- **VSIX size.** The package contains the entire published WebAssembly app (runtime, assemblies, ICU data, the Seagull
  font and metadata) plus a PyInstaller `pyftsubset` binary, so expect tens of megabytes per platform. Publish with
  AOT disabled and IL trimming enabled to keep it manageable.
- **Platform-specific packages.** Because `pyftsubset` is a native binary, one VSIX is built per platform
  (`win32-x64`, `linux-x64`, `linux-arm64`, `darwin-x64`, `darwin-arm64`). Other platforms (e.g. musl-based Linux)
  can still use the extension if `pyftsubset` or Python with fonttools is on the PATH.
- **Remote workspaces.** The extension runs where the workspace files live (workspace extension host), which is also
  where `pyftsubset` runs, so exports in Remote SSH / WSL / Codespaces sessions require a Linux binary there. The
  platform-specific packaging takes care of this for the supported platforms.
- **One editor per document.** `supportsMultipleEditorsPerDocument` is off to avoid booting multiple runtimes for the
  same file.
