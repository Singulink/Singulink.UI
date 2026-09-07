import * as crypto from 'node:crypto';
import * as fs from 'node:fs/promises';
import * as vscode from 'vscode';

/**
 * Builds the HTML for the editor webview.
 *
 * The published `index.html` cannot be used as-is because the webview needs a nonce-based CSP, a `<base>` so relative
 * asset URLs resolve against the extension's resource root, and the host bridge installed before the app starts. The
 * tags below mirror what Uno.Wasm.Bootstrap generates: `require.js`, the `uno-bootstrap.js` module (which loads
 * `_framework/dotnet.js` itself) and its stylesheets, all of which live in a content-hashed `package_<hash>/` folder
 * whose name is discovered at runtime.
 */
export async function getWebviewHtml(webview: vscode.Webview, extensionUri: vscode.Uri): Promise<string> {
    const appRoot = vscode.Uri.joinPath(extensionUri, 'media', 'app');
    const appBaseUri = webview.asWebviewUri(appRoot);
    const packageDir = await findPackageDir(appRoot);
    const nonce = crypto.randomBytes(16).toString('base64');
    const cspSource = webview.cspSource;

    // 'wasm-unsafe-eval' is required to instantiate dotnet.wasm and 'unsafe-eval' because Uno's WebAssembly runtime
    // evaluates JavaScript strings for its interop layer (WebAssemblyRuntime.InvokeJS); without it the UI dispatcher
    // fails on its first call. 'unsafe-inline' for styles is required because the Uno/Skia renderer injects inline
    // style attributes. Scripts from the resource origin are allowed without a nonce because require.js and the .NET
    // loader inject further script tags themselves.
    const csp = [
        `default-src 'none'`,
        `img-src ${cspSource} blob: data:`,
        `style-src ${cspSource} 'unsafe-inline'`,
        `font-src ${cspSource} data:`,
        `script-src 'nonce-${nonce}' ${cspSource} 'wasm-unsafe-eval' 'unsafe-eval'`,
        `connect-src ${cspSource} data: blob:`,
        `worker-src ${cspSource} blob:`,
    ].join('; ') + ';';

    const appTags = packageDir === undefined
        ? `<script nonce="${nonce}">document.querySelector('.uno-loader .alert').textContent = ${JSON.stringify(MISSING_APP_MESSAGE)};</script>`
        : [
            `<link rel="stylesheet" type="text/css" href="./${packageDir}/normalize.css" />`,
            `<link rel="stylesheet" type="text/css" href="./${packageDir}/uno-bootstrap.css" />`,
            `<link rel="stylesheet" type="text/css" href="./${packageDir}/uno.css" />`,
            `<link rel="stylesheet" type="text/css" href="./${packageDir}/Fonts.css" />`,
            `<script nonce="${nonce}" src="./${packageDir}/require.js"></script>`,
            `<script type="module" nonce="${nonce}" src="./${packageDir}/uno-bootstrap.js"></script>`,
        ].join('\n    ');

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta http-equiv="Content-Security-Policy" content="${csp}" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="${appBaseUri}/" />
    <title>Icon Pack Builder</title>
    <style nonce="${nonce}">
        html, body {
            margin: 0;
            padding: 0;
            height: 100%;
            width: 100%;
            overflow: hidden;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
        }

        #uno-body {
            position: absolute;
            inset: 0;
        }

        /* Uno sets the splash color inline from the app manifest; follow the VS Code theme instead so dark themes never flash a light splash. */
        .uno-loader {
            background-color: var(--vscode-editor-background) !important;
        }

        .uno-loader .alert {
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
            white-space: pre-wrap;
        }
    </style>
    <script nonce="${nonce}">${BRIDGE_SCRIPT}</script>
</head>
<body>
    <!-- Same structure as Uno's generated index.html: the bootstrapper requires the loader, its image and the progress bar. -->
    <div id="uno-body" class="container-fluid uno-body">
        <div class="uno-loader" loading-position="bottom" loading-alert="none">
            <img class="logo" src="" title="Loading Icon Pack Builder" />
            <progress></progress>
            <span class="alert"></span>
        </div>
    </div>
    ${appTags}
</body>
</html>`;
}

const MISSING_APP_MESSAGE =
    'The Icon Pack Builder app files are missing from this extension build (media/app has no package_* folder). ' +
    'See IconPackBuilder.VSCode/README.md for how to publish the WebAssembly head and package the extension.';

/**
 * Finds the content-hashed Uno package folder (`package_<hash>`) in the published app, or undefined if the app has not
 * been copied into the extension.
 */
async function findPackageDir(appRoot: vscode.Uri): Promise<string | undefined> {
    try {
        const entries = await fs.readdir(appRoot.fsPath, { withFileTypes: true });
        return entries.find((entry) => entry.isDirectory() && entry.name.startsWith('package_'))?.name;
    }
    catch {
        return undefined;
    }
}

/**
 * Bridge between the .NET app and the extension host, installed as `window.ipbHost` before the app script runs.
 *
 * This is a plain (non-module) script so it executes synchronously during parsing, guaranteeing the bridge exists by
 * the time the deferred module script for the app starts. The `acquireVsCodeApi` result is kept inside the closure so
 * the app cannot post arbitrary messages to the host.
 *
 * Note: this string is embedded into a template literal, so it must not contain backticks or "\${".
 */
const BRIDGE_SCRIPT = `
(function () {
    'use strict';

    const vscode = acquireVsCodeApi();

    let documentText = '';
    let documentFileName = '';
    let documentDir = '';
    let themeKind = 'dark';
    let nextRequestId = 1;

    const changeCallbacks = [];
    const themeCallbacks = [];
    const pendingRequests = new Map();

    let resolveInit;
    let initReceived = false;
    const initPromise = new Promise(function (resolve) { resolveInit = resolve; });

    // Forward console output and unhandled errors to the extension host: the webview's own console is only visible
    // through the developer tools, and boot failures of the .NET runtime would otherwise be silent.
    function forwardLog(level, args) {
        try {
            const message = Array.prototype.map.call(args, function (a) {
                if (a instanceof Error) { return a.stack || a.message; }
                if (typeof a === 'string') { return a; }
                try { return JSON.stringify(a); } catch (e) { return String(a); }
            }).join(' ');
            vscode.postMessage({ type: 'log', level: level, message: message });
        }
        catch (e) {
            // Never let diagnostics interfere with the app.
        }
    }

    ['log', 'warn', 'error'].forEach(function (level) {
        const original = console[level].bind(console);
        console[level] = function () {
            original.apply(console, arguments);
            forwardLog(level, arguments);
        };
    });

    window.addEventListener('error', function (event) {
        forwardLog('error', ['Unhandled error: ' + (event.message || ''), event.error || '']);
    });

    window.addEventListener('unhandledrejection', function (event) {
        forwardLog('error', ['Unhandled promise rejection:', event.reason || '']);
    });

    function sendRequest(type, payload) {
        const requestId = nextRequestId++;
        return new Promise(function (resolve, reject) {
            pendingRequests.set(requestId, { resolve: resolve, reject: reject });
            vscode.postMessage(Object.assign({ type: type, requestId: requestId }, payload));
        });
    }

    function completeRequest(requestId, value) {
        const pending = pendingRequests.get(requestId);
        if (!pending) {
            return;
        }
        pendingRequests.delete(requestId);
        pending.resolve(value);
    }

    function hideLoadingIndicator() {
        // Uno removes its own loader when the app starts; this only covers the case where the app wants it gone earlier.
        const loading = document.querySelector('.uno-loader');
        if (loading) {
            loading.remove();
        }
    }

    window.addEventListener('message', function (event) {
        const message = event.data;
        if (!message || typeof message.type !== 'string') {
            return;
        }

        switch (message.type) {
            case 'init':
                documentText = message.text;
                documentFileName = message.fileName;
                documentDir = message.dir;
                themeKind = message.themeKind || themeKind;
                initReceived = true;
                console.debug('[ipbHost] init received for ' + message.fileName);
                resolveInit();
                break;

            case 'themeChanged':
                themeKind = message.themeKind || themeKind;
                for (const callback of themeCallbacks) {
                    try {
                        callback(themeKind);
                    }
                    catch (e) {
                        console.error('[ipbHost] onThemeChanged callback failed', e);
                    }
                }
                break;

            case 'documentChanged':
                documentText = message.text;
                for (const callback of changeCallbacks) {
                    try {
                        callback(message.text);
                    }
                    catch (e) {
                        console.error('[ipbHost] onDocumentChanged callback failed', e);
                    }
                }
                break;

            case 'exportResult':
                completeRequest(message.requestId, JSON.stringify({
                    ok: message.ok,
                    exportDir: message.exportDir,
                    error: message.error === undefined ? null : message.error
                }));
                break;

            case 'showMessageResult':
                completeRequest(message.requestId, message.index);
                break;
        }
    });

    window.ipbHost = Object.freeze({
        /** Resolves once the host has delivered the initial document (the 'init' message). */
        whenReady: function () {
            console.debug('[ipbHost] whenReady called by the app' + (initReceived ? ' (already initialized)' : ''));
            return initPromise;
        },

        /** Latest document text received from the host (or set via setDocumentText). */
        getDocumentText: function () {
            return documentText;
        },

        /** Replaces the whole document with the given text. Does not save; VS Code owns dirty state and saving. */
        setDocumentText: function (text) {
            documentText = text;
            vscode.postMessage({ type: 'edit', text: text });
        },

        /** Registers a callback invoked with the new text whenever the document changes outside the app. */
        onDocumentChanged: function (callback) {
            changeCallbacks.push(callback);
        },

        /**
         * Exports the project. requestJson is a JSON string with the shape:
         *   { projectName, exportDirName, fontFileName, fontBase64, codePoints: number[], files: [{ name, contentBase64 }] }
         * Resolves with a JSON string: { ok: boolean, exportDir: string, error: string | null }.
         */
        exportProject: function (requestJson) {
            console.debug('[ipbHost] exportProject called');
            return sendRequest('export', { request: JSON.parse(requestJson) });
        },

        /**
         * Shows a modal VS Code message. buttonsJson is a JSON array of button labels. Resolves with the index of the
         * chosen button, or -1 if the dialog was dismissed.
         */
        showMessage: function (message, title, buttonsJson) {
            return sendRequest('showMessage', {
                message: message,
                title: title || undefined,
                buttons: buttonsJson ? JSON.parse(buttonsJson) : []
            });
        },

        /** File name (without directory) of the open project document. */
        getDocumentFileName: function () {
            return documentFileName;
        },

        /** Directory of the open project document, or an empty string if it is not saved on disk. */
        getDocumentDir: function () {
            return documentDir;
        },

        /** VS Code color theme kind: 'light', 'dark', 'highContrast' or 'highContrastLight'. */
        getThemeKind: function () {
            return themeKind;
        },

        /** Registers a callback invoked with the new theme kind when the VS Code color theme changes. */
        onThemeChanged: function (callback) {
            themeCallbacks.push(callback);
        },

        /** Removes the loading indicator early; Uno removes it itself once the app has started. */
        hideLoadingIndicator: hideLoadingIndicator
    });

    // Announce readiness until the host answers with 'init'. The first message can be lost if the host is still
    // wiring up its listener when the page starts executing.
    (function announceReady(attempt) {
        if (initReceived) {
            return;
        }
        vscode.postMessage({ type: 'ready' });
        if (attempt < 60) {
            setTimeout(function () { announceReady(attempt + 1); }, 500);
        }
        else {
            console.error('[ipbHost] The extension host did not deliver the document after 30 seconds.');
        }
    })(0);
})();
`;
