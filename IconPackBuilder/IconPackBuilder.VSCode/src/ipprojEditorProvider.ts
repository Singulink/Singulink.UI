import * as fs from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import * as vscode from 'vscode';
import { runPyftsubset } from './pyftsubset';
import { getWebviewHtml } from './webviewHtml';

/**
 * Messages posted by the webview (the bridge script in webviewHtml.ts) to the extension host.
 */
type WebviewMessage =
    | { type: 'ready' }
    | { type: 'log'; level: 'log' | 'warn' | 'error'; message: string }
    | { type: 'edit'; text: string }
    | { type: 'export'; requestId: number; request: ExportRequest }
    | { type: 'showMessage'; requestId: number; message: string; title?: string; buttons?: string[] };

/**
 * Messages posted by the extension host to the webview.
 */
type HostMessage =
    | { type: 'init'; text: string; fileName: string; dir: string; themeKind: ThemeKind }
    | { type: 'documentChanged'; text: string }
    | { type: 'themeChanged'; themeKind: ThemeKind }
    | { type: 'exportResult'; requestId: number; ok: boolean; exportDir: string; error?: string }
    | { type: 'showMessageResult'; requestId: number; index: number };

/** VS Code color theme kind as seen by the app. */
type ThemeKind = 'light' | 'dark' | 'highContrast' | 'highContrastLight';

function getThemeKind(theme: vscode.ColorTheme = vscode.window.activeColorTheme): ThemeKind {
    switch (theme.kind) {
        case vscode.ColorThemeKind.Light: return 'light';
        case vscode.ColorThemeKind.HighContrast: return 'highContrast';
        case vscode.ColorThemeKind.HighContrastLight: return 'highContrastLight';
        default: return 'dark';
    }
}

/**
 * Export request produced by the .NET app. The app does all the icon/code point resolution itself; the host only
 * writes files and runs the font subsetter, which cannot be done from inside the webview sandbox.
 */
interface ExportRequest {
    projectName: string;
    exportDirName: string;
    fontFileName: string;
    fontBase64: string;
    codePoints: number[];
    files: { name: string; contentBase64: string }[];
}

/**
 * Custom editor for `.ipproj` files that hosts the Uno WebAssembly build of Icon Pack Builder in a webview.
 *
 * The project file is JSON text, so this is a {@link vscode.CustomTextEditorProvider}: VS Code owns the document,
 * dirty state, save, undo/redo and external change reloads. The webview receives the full document text and sends
 * back full replacement text whenever the app changes the project.
 */
export class IpprojEditorProvider implements vscode.CustomTextEditorProvider {
    public static readonly viewType = 'singulink.iconPackBuilder';

    /** Output channel receiving console output forwarded from the webview (the app's own console is otherwise invisible). */
    public static readonly output = vscode.window.createOutputChannel('Icon Pack Builder');

    /** URI of the document shown in the custom editor that most recently had focus, if any. Maintained by EditorSession. */
    public static activeDocumentUri: vscode.Uri | undefined;

    public static register(context: vscode.ExtensionContext): vscode.Disposable {
        context.subscriptions.push(IpprojEditorProvider.output);

        return vscode.window.registerCustomEditorProvider(
            IpprojEditorProvider.viewType,
            new IpprojEditorProvider(context),
            {
                // Booting the .NET runtime is expensive, so keep the webview alive while its tab is hidden.
                webviewOptions: { retainContextWhenHidden: true },
                supportsMultipleEditorsPerDocument: false,
            },
        );
    }

    private constructor(private readonly context: vscode.ExtensionContext) { }

    public async resolveCustomTextEditor(
        document: vscode.TextDocument,
        webviewPanel: vscode.WebviewPanel,
        _token: vscode.CancellationToken,
    ): Promise<void> {
        const session = new EditorSession(this.context, document, webviewPanel);
        await session.start();
    }
}

/**
 * State for one open editor instance (one document + one webview panel).
 */
class EditorSession {
    private readonly disposables: vscode.Disposable[] = [];

    /**
     * The document text the webview is known to have. Used to avoid echoing the webview's own edits back to it:
     * when the document changes, the new text is only pushed if it differs from this value.
     */
    private webviewText: string;

    constructor(
        private readonly context: vscode.ExtensionContext,
        private readonly document: vscode.TextDocument,
        private readonly panel: vscode.WebviewPanel,
    ) {
        this.webviewText = document.getText();
    }

    public async start(): Promise<void> {
        const appRoot = vscode.Uri.joinPath(this.context.extensionUri, 'media', 'app');

        this.panel.webview.options = {
            enableScripts: true,
            localResourceRoots: [appRoot],
        };

        // The bridge script posts 'ready' as soon as it runs, so the handler must exist before the HTML is set or the
        // message is lost and the app never receives its document.
        this.disposables.push(
            this.panel.webview.onDidReceiveMessage((message: WebviewMessage) => void this.handleMessage(message)),
        );

        this.panel.webview.html = await getWebviewHtml(this.panel.webview, this.context.extensionUri);

        this.disposables.push(
            vscode.window.onDidChangeActiveColorTheme(theme => this.post({ type: 'themeChanged', themeKind: getThemeKind(theme) })),
        );

        this.disposables.push(
            vscode.workspace.onDidChangeTextDocument(e => {
                if (e.document.uri.toString() !== this.document.uri.toString()) {
                    return;
                }

                const text = e.document.getText();

                // Skip changes that originated from the webview (the edit we just applied on its behalf).
                if (text === this.webviewText) {
                    return;
                }

                this.webviewText = text;
                this.post({ type: 'documentChanged', text });
            }),
        );

        this.disposables.push(
            this.panel.onDidChangeViewState(e => {
                if (e.webviewPanel.active) {
                    IpprojEditorProvider.activeDocumentUri = this.document.uri;
                }
            }),
        );

        if (this.panel.active) {
            IpprojEditorProvider.activeDocumentUri = this.document.uri;
        }

        this.disposables.push(this.panel.onDidDispose(() => this.dispose()));
    }

    private dispose(): void {
        if (IpprojEditorProvider.activeDocumentUri?.toString() === this.document.uri.toString()) {
            IpprojEditorProvider.activeDocumentUri = undefined;
        }

        for (const d of this.disposables.splice(0)) {
            d.dispose();
        }
    }

    private post(message: HostMessage): void {
        void this.panel.webview.postMessage(message);
    }

    private async handleMessage(message: WebviewMessage): Promise<void> {
        switch (message.type) {
            case 'ready':
                // The webview always reports ready after (re)loading, so send the current document state rather than
                // whatever it had before.
                IpprojEditorProvider.output.appendLine(`[host] ready received from webview for ${this.document.uri.fsPath}; sending init`);
                this.webviewText = this.document.getText();
                this.post({
                    type: 'init',
                    text: this.webviewText,
                    fileName: path.basename(this.document.uri.path),
                    dir: this.getDocumentDir() ?? '',
                    themeKind: getThemeKind(),
                });
                break;

            case 'log':
                IpprojEditorProvider.output.appendLine(`[${message.level}] ${message.message}`);
                break;

            case 'edit':
                await this.applyEdit(message.text);
                break;

            case 'export':
                await this.handleExport(message.requestId, message.request);
                break;

            case 'showMessage':
                await this.handleShowMessage(message.requestId, message.message, message.title, message.buttons ?? []);
                break;

            default:
                console.warn('[IconPackBuilder] Unknown webview message', message);
                break;
        }
    }

    private async applyEdit(text: string): Promise<void> {
        if (text === this.document.getText()) {
            this.webviewText = text;
            return;
        }

        // Record the incoming text before applying so the resulting onDidChangeTextDocument event is recognised as
        // our own and not echoed back.
        this.webviewText = text;

        const fullRange = new vscode.Range(
            this.document.positionAt(0),
            this.document.positionAt(this.document.getText().length),
        );

        const edit = new vscode.WorkspaceEdit();
        edit.replace(this.document.uri, fullRange, text);

        const applied = await vscode.workspace.applyEdit(edit);

        if (!applied) {
            // Resync the webview with what the document actually contains.
            this.webviewText = this.document.getText();
            this.post({ type: 'documentChanged', text: this.webviewText });
        }
    }

    /** Directory of the document on disk, or undefined for documents that are not files (e.g. untitled). */
    private getDocumentDir(): string | undefined {
        return this.document.uri.scheme === 'file' ? path.dirname(this.document.uri.fsPath) : undefined;
    }

    private async handleExport(requestId: number, request: ExportRequest): Promise<void> {
        let exportDir = '';

        try {
            const documentDir = this.getDocumentDir();

            if (!documentDir) {
                throw new Error('The project must be saved to disk before it can be exported.');
            }

            validateExportRequest(request);
            exportDir = path.join(documentDir, request.exportDirName);

            await this.writeExport(exportDir, request);

            this.post({ type: 'exportResult', requestId, ok: true, exportDir });

            const reveal = 'Reveal in Explorer';
            const choice = await vscode.window.showInformationMessage(`Exported to ${exportDir}`, reveal);

            if (choice === reveal) {
                await vscode.commands.executeCommand('revealFileInOS', vscode.Uri.file(exportDir));
            }
        }
        catch (err) {
            const error = err instanceof Error ? err.message : String(err);
            this.post({ type: 'exportResult', requestId, ok: false, exportDir, error });
            void vscode.window.showErrorMessage(`Icon pack export failed: ${error}`);
        }
    }

    private async writeExport(exportDir: string, request: ExportRequest): Promise<void> {
        const tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'ipb-export-'));

        try {
            const sourceFontPath = path.join(tempDir, 'source' + (path.extname(request.fontFileName) || '.otf'));
            const unicodesPath = path.join(tempDir, 'unicodes.txt');

            await fs.writeFile(sourceFontPath, Buffer.from(request.fontBase64, 'base64'));

            const unicodes = request.codePoints
                .map(cp => 'U+' + cp.toString(16).toUpperCase().padStart(4, '0'))
                .join('\n');

            await fs.writeFile(unicodesPath, unicodes, 'utf8');

            // Remove stale output from previous exports (renamed icons, removed files, ...) before writing.
            await fs.rm(exportDir, { recursive: true, force: true });
            await fs.mkdir(exportDir, { recursive: true });

            for (const file of request.files) {
                await fs.writeFile(path.join(exportDir, file.name), Buffer.from(file.contentBase64, 'base64'));
            }

            await runPyftsubset(this.context.extensionPath, [
                sourceFontPath,
                `--unicodes-file=${unicodesPath}`,
                `--output-file=${path.join(exportDir, request.fontFileName)}`,
            ]);
        }
        finally {
            await fs.rm(tempDir, { recursive: true, force: true }).catch(() => undefined);
        }
    }

    private async handleShowMessage(requestId: number, message: string, title: string | undefined, buttons: string[]): Promise<void> {
        // Use objects rather than plain strings so identical button labels still map back to the right index.
        const items = buttons.map((label, index) => ({ title: label, index }));

        // In modal dialogs the first argument is rendered as the bold heading and `detail` as the body text.
        const choice = title
            ? await vscode.window.showInformationMessage(title, { modal: true, detail: message }, ...items)
            : await vscode.window.showInformationMessage(message, { modal: true }, ...items);

        this.post({ type: 'showMessageResult', requestId, index: choice?.index ?? -1 });
    }
}

/**
 * Rejects requests that could write outside the export directory or that are otherwise malformed.
 */
function validateExportRequest(request: ExportRequest): void {
    if (!request || typeof request !== 'object') {
        throw new Error('Invalid export request.');
    }

    assertSafeFileName(request.exportDirName, 'export directory name');
    assertSafeFileName(request.fontFileName, 'font file name');

    if (typeof request.fontBase64 !== 'string' || request.fontBase64.length === 0) {
        throw new Error('The export request does not contain the source font.');
    }

    if (!Array.isArray(request.codePoints) || request.codePoints.length === 0) {
        throw new Error('The project does not contain any icons to export.');
    }

    if (!request.codePoints.every(cp => Number.isInteger(cp) && cp >= 0 && cp <= 0x10FFFF)) {
        throw new Error('The export request contains an invalid code point.');
    }

    if (!Array.isArray(request.files)) {
        throw new Error('The export request does not contain a file list.');
    }

    for (const file of request.files) {
        assertSafeFileName(file?.name, 'output file name');

        if (typeof file.contentBase64 !== 'string') {
            throw new Error(`Output file '${file.name}' has no content.`);
        }
    }
}

function assertSafeFileName(name: unknown, description: string): asserts name is string {
    if (typeof name !== 'string' || name.length === 0 || name === '.' || name === '..' || path.basename(name) !== name) {
        throw new Error(`Invalid ${description}: '${String(name)}'.`);
    }
}
