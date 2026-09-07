import * as vscode from 'vscode';
import { IpprojEditorProvider } from './ipprojEditorProvider';
import { newProject } from './newProject';

export function activate(context: vscode.ExtensionContext): void {
    context.subscriptions.push(IpprojEditorProvider.register(context));

    context.subscriptions.push(
        vscode.commands.registerCommand('singulink.iconPackBuilder.newProject', (folder?: vscode.Uri) => newProject(context, folder)),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('singulink.iconPackBuilder.openWithTextEditor', async (uri?: vscode.Uri) => {
            // When invoked from the editor title menu VS Code passes the resource URI; from the command palette it does not,
            // so fall back to the custom editor that most recently had focus.
            const target = uri ?? IpprojEditorProvider.activeDocumentUri ?? vscode.window.activeTextEditor?.document.uri;

            if (!target) {
                void vscode.window.showWarningMessage('No Icon Pack Builder project is currently open.');
                return;
            }

            await vscode.commands.executeCommand('vscode.openWith', target, 'default');
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('singulink.iconPackBuilder.openWithIconPackBuilder', async (uri?: vscode.Uri) => {
            const target = uri ?? vscode.window.activeTextEditor?.document.uri;

            if (!target) {
                void vscode.window.showWarningMessage('No .ipproj file is currently open.');
                return;
            }

            await vscode.commands.executeCommand('vscode.openWith', target, IpprojEditorProvider.viewType);
        }),
    );
}

export function deactivate(): void {
    // Nothing to do; all resources are registered on the extension context.
}
