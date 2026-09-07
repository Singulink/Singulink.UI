import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import * as vscode from 'vscode';
import { IpprojEditorProvider } from './ipprojEditorProvider';

/** Identifier of the icon source bundled with the extension; must match `SeagullIconsSource.Id` in the app. */
const ICONS_SOURCE_ID = 'FluentIcons.Seagull';

/**
 * Creates a new `.ipproj` file and opens it in the Icon Pack Builder editor. Mirrors the start screen of the desktop app: the project name must be
 * in `Namespace.Class` form because it becomes the namespace and class name of the generated C# code.
 *
 * @param targetFolder Folder to create the project in (from the Explorer context menu); when omitted the user picks one.
 */
export async function newProject(context: vscode.ExtensionContext, targetFolder?: vscode.Uri): Promise<void> {
    const name = await vscode.window.showInputBox({
        title: 'New Icon Pack Project',
        prompt: 'Project name in Namespace.Class form. It becomes the namespace and class name of the generated C# icon class.',
        placeHolder: 'MyApp.FontIcons',
        validateInput: validateProjectName,
    });

    if (!name) {
        return;
    }

    const folder = targetFolder ?? await pickFolder();

    if (!folder) {
        return;
    }

    const trimmedName = name.trim();
    const fileUri = vscode.Uri.joinPath(folder, `${trimmedName}.ipproj`);

    if (await exists(fileUri.fsPath)) {
        void vscode.window.showErrorMessage(`'${path.basename(fileUri.fsPath)}' already exists in ${folder.fsPath}.`);
        return;
    }

    // Same shape and key casing as IconPackBuilder.Data.Project so the desktop app and the extension can open each other's files.
    const project = {
        Name: trimmedName,
        IconsSourceId: ICONS_SOURCE_ID,
        IconsSourceVersion: await readBundledIconsSourceVersion(context),
        IconExports: [],
    };

    await fs.writeFile(fileUri.fsPath, JSON.stringify(project, null, 2) + '\n', 'utf8');
    await vscode.commands.executeCommand('vscode.openWith', fileUri, IpprojEditorProvider.viewType);
}

/**
 * Validates a `Namespace.Class` project name the same way the desktop app does: at least two dot-separated ASCII identifiers.
 */
export function validateProjectName(value: string): string | undefined {
    const trimmed = value.trim();

    if (trimmed.length === 0) {
        return 'Enter a project name.';
    }

    const segments = trimmed.split('.');

    if (segments.length < 2) {
        return 'Use Namespace.Class form, for example MyApp.FontIcons.';
    }

    if (!segments.every(segment => /^[A-Za-z][A-Za-z0-9_]*$/.test(segment))) {
        return 'Each part must start with a letter and contain only letters, digits and underscores.';
    }

    return undefined;
}

async function pickFolder(): Promise<vscode.Uri | undefined> {
    const folders = vscode.workspace.workspaceFolders ?? [];

    if (folders.length === 1) {
        return folders[0].uri;
    }

    const picked = await vscode.window.showOpenDialog({
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
        openLabel: 'Create Project Here',
        defaultUri: folders[0]?.uri,
    });

    return picked?.[0];
}

/**
 * Reads the Seagull icon metadata version from the bundled app so new projects record the source version they were created against, which the
 * editor uses to warn about downgrades.
 */
async function readBundledIconsSourceVersion(context: vscode.ExtensionContext): Promise<string> {
    try {
        const appDir = path.join(context.extensionPath, 'media', 'app');
        const packageDir = (await fs.readdir(appDir, { withFileTypes: true })).find(e => e.isDirectory() && e.name.startsWith('package_'));

        if (!packageDir) {
            return '0.0';
        }

        const dataPath = path.join(appDir, packageDir.name, 'Assets', 'Seagull', 'SeagullFluentIcons.json');
        const data = JSON.parse(await fs.readFile(dataPath, 'utf8')) as { version?: string };
        return typeof data.version === 'string' && data.version.length > 0 ? data.version : '0.0';
    }
    catch {
        return '0.0';
    }
}

async function exists(filePath: string): Promise<boolean> {
    try {
        await fs.access(filePath);
        return true;
    }
    catch {
        return false;
    }
}
