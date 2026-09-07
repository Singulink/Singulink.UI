#!/usr/bin/env node
// Builds platform-specific VSIX packages, each bundling the matching pyftsubset binary.
//
// Usage:
//   node scripts/package.mjs --bin-dir <dir> [--app-dir <wwwroot>] [--targets win32-x64,linux-x64,...] [--out dist]
//
// --bin-dir   Directory containing the pyftsubset binaries named by .NET runtime identifier:
//             pyftsubset-win-x64.exe, pyftsubset-linux-x64, pyftsubset-linux-arm64, pyftsubset-osx-x64, pyftsubset-osx-arm64
// --app-dir   Published Uno WebAssembly output (the wwwroot folder). It is copied into media/app/ (cleared first).
//             If omitted, the existing media/app/ contents are used.
// --targets   Comma-separated subset of the VS Code targets below. Defaults to all of them.
// --out       Output directory for the .vsix files. Defaults to dist/.

import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const extensionRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(import.meta.url);

/** VS Code platform target -> .NET RID used in the binary file names. */
const TARGETS = {
    'win32-x64': { rid: 'win-x64', binaryName: 'pyftsubset.exe' },
    'linux-x64': { rid: 'linux-x64', binaryName: 'pyftsubset' },
    'linux-arm64': { rid: 'linux-arm64', binaryName: 'pyftsubset' },
    'darwin-x64': { rid: 'osx-x64', binaryName: 'pyftsubset' },
    'darwin-arm64': { rid: 'osx-arm64', binaryName: 'pyftsubset' },
};

function fail(message) {
    console.error(`error: ${message}`);
    process.exit(1);
}

function parseArgs(argv) {
    const options = { binDir: undefined, appDir: undefined, targets: Object.keys(TARGETS), out: 'dist' };

    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        const value = () => {
            if (i + 1 >= argv.length) {
                fail(`${arg} requires a value.`);
            }
            return argv[++i];
        };

        switch (arg) {
            case '--bin-dir': options.binDir = path.resolve(value()); break;
            case '--app-dir': options.appDir = path.resolve(value()); break;
            case '--out': options.out = value(); break;
            case '--targets':
                options.targets = value().split(',').map(t => t.trim()).filter(Boolean);
                break;
            case '--help':
            case '-h':
                console.log(fs.readFileSync(fileURLToPath(import.meta.url), 'utf8').split('\n')
                    .filter(line => line.startsWith('//')).map(line => line.slice(3)).join('\n'));
                process.exit(0);
                break;
            default:
                fail(`Unknown argument '${arg}'.`);
        }
    }

    for (const target of options.targets) {
        if (!TARGETS[target]) {
            fail(`Unknown target '${target}'. Valid targets: ${Object.keys(TARGETS).join(', ')}.`);
        }
    }

    if (!options.binDir) {
        fail('--bin-dir <dir> is required (directory containing pyftsubset-<rid>[.exe] binaries).');
    }

    if (!fs.existsSync(options.binDir) || !fs.statSync(options.binDir).isDirectory()) {
        fail(`--bin-dir '${options.binDir}' does not exist or is not a directory.`);
    }

    return options;
}

function copyApp(appDir, mediaAppDir) {
    if (!fs.existsSync(appDir) || !fs.statSync(appDir).isDirectory()) {
        fail(`--app-dir '${appDir}' does not exist or is not a directory.`);
    }

    if (!fs.existsSync(path.join(appDir, '_framework'))) {
        fail(`--app-dir '${appDir}' does not look like published WebAssembly output (no _framework folder). ` +
            'Point it at the wwwroot folder of the publish output.');
    }

    console.log(`Copying app from ${appDir} to ${mediaAppDir}`);
    fs.rmSync(mediaAppDir, { recursive: true, force: true });
    fs.mkdirSync(mediaAppDir, { recursive: true });
    // Uno publishes pre-compressed (.br/.gz) copies of every file for static hosting; the webview serves raw files, so they
    // would only inflate the VSIX.
    fs.cpSync(appDir, mediaAppDir, { recursive: true, filter: (source) => !/\.(br|gz)$/i.test(source) });
}

function verifyApp(mediaAppDir) {
    if (!fs.existsSync(mediaAppDir) || fs.readdirSync(mediaAppDir).length === 0) {
        fail(`${mediaAppDir} is empty. Publish the WebAssembly head and pass --app-dir <wwwroot>.`);
    }

    if (!fs.existsSync(path.join(mediaAppDir, '_framework'))) {
        fail(`${mediaAppDir} does not contain a _framework folder; is it the published wwwroot output?`);
    }

    patchUnoConfig(mediaAppDir);
}

/**
 * Makes the Uno bootstrapper configuration loadable from the webview.
 *
 * Uno generates `uno-config.js` for hosting at a site root: `uno_app_base` and every dependency are absolute paths such as
 * `/package_<hash>/Uno.Wasm.js`. Inside the webview the page origin is `vscode-webview://...` while the app files are served
 * from the extension's resource URI (the `<base href>` of the generated HTML), so root-absolute paths 404 and the app never
 * boots. Rewriting them to `./package_<hash>/...` makes them resolve against the base URI. The PWA service worker is disabled
 * as well: webviews cannot register one and the attempt only produces console errors. The patch is idempotent.
 */
function patchUnoConfig(mediaAppDir) {
    const packageDir = fs.readdirSync(mediaAppDir, { withFileTypes: true })
        .find((entry) => entry.isDirectory() && entry.name.startsWith('package_'));

    if (!packageDir) {
        fail(`${mediaAppDir} does not contain a package_<hash> folder; is it the published wwwroot output?`);
    }

    const configPath = path.join(mediaAppDir, packageDir.name, 'uno-config.js');

    if (!fs.existsSync(configPath)) {
        fail(`${configPath} not found; the Uno bootstrapper layout may have changed.`);
    }

    const original = fs.readFileSync(configPath, 'utf8');
    // UNO_BOOTSTRAP_WEBAPP_BASE_PATH is what Uno prefixes to ms-appx asset fetches (fonts, icon metadata) at runtime.
    // uno_shell_mode: the bootstrapper treats any user agent containing "Electron" (which VS Code's is) as a Node/Electron
    // host and switches to synchronous CommonJS require() for its dependencies, which fails in a webview. "BrowserEmbedded"
    // is the only value that disables that check, and it is not used for anything else.
    const patched = original
        .replace(/(["'])\/package_/g, '$1./package_')
        .replace(/(environmentVariables\["UNO_BOOTSTRAP_WEBAPP_BASE_PATH"\]\s*=\s*)"\/"/, '$1"./"')
        .replace(/config\.uno_shell_mode\s*=\s*"Browser";/, 'config.uno_shell_mode = "BrowserEmbedded";')
        .replace(/config\.enable_pwa\s*=\s*true;/, 'config.enable_pwa = false;');

    if (patched !== original) {
        fs.writeFileSync(configPath, patched, 'utf8');
        console.log(`Patched ${path.relative(mediaAppDir, configPath)} for webview hosting (relative paths, PWA disabled).`);
    }

    // The generated splash PNG is flattened onto the splash color, which never matches the VS Code theme exactly. The app
    // icon keeps its transparent corners, so use that as the loader image instead.
    const manifestPath = path.join(mediaAppDir, packageDir.name, 'AppManifest.js');
    const iconName = ['icon-512.png', 'icon-128.png'].find(name => fs.existsSync(path.join(mediaAppDir, packageDir.name, name)));

    if (fs.existsSync(manifestPath) && iconName) {
        const manifest = fs.readFileSync(manifestPath, 'utf8');
        const patchedManifest = manifest.replace(/splashScreenImage\s*:\s*"[^"]*"/, `splashScreenImage: "${iconName}"`);

        if (patchedManifest !== manifest) {
            fs.writeFileSync(manifestPath, patchedManifest, 'utf8');
            console.log(`Patched ${path.relative(mediaAppDir, manifestPath)} to use ${iconName} as the splash image.`);
        }
    }
}

function resolveVsce() {
    let packageJsonPath;

    try {
        packageJsonPath = require.resolve('@vscode/vsce/package.json');
    }
    catch {
        fail('@vscode/vsce is not installed. Run `npm install` first.');
    }

    const pkg = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));
    const bin = typeof pkg.bin === 'string' ? pkg.bin : pkg.bin?.vsce;

    if (!bin) {
        fail('Could not locate the vsce executable in @vscode/vsce.');
    }

    return path.join(path.dirname(packageJsonPath), bin);
}

function main() {
    const options = parseArgs(process.argv.slice(2));
    const mediaAppDir = path.join(extensionRoot, 'media', 'app');
    const binDir = path.join(extensionRoot, 'bin');
    const outDir = path.resolve(extensionRoot, options.out);

    if (options.appDir) {
        copyApp(options.appDir, mediaAppDir);
    }

    verifyApp(mediaAppDir);

    // Check all binaries up front so a missing one fails before any packaging work happens.
    const sources = {};

    for (const target of options.targets) {
        const { rid } = TARGETS[target];
        const ext = rid.startsWith('win') ? '.exe' : '';
        const source = path.join(options.binDir, `pyftsubset-${rid}${ext}`);

        if (!fs.existsSync(source)) {
            fail(`Missing pyftsubset binary for ${target}: ${source}`);
        }

        sources[target] = source;
    }

    const vsce = resolveVsce();
    fs.mkdirSync(outDir, { recursive: true });

    for (const target of options.targets) {
        const { binaryName } = TARGETS[target];

        console.log(`\n=== Packaging ${target} ===`);

        fs.rmSync(binDir, { recursive: true, force: true });
        fs.mkdirSync(binDir, { recursive: true });

        const destination = path.join(binDir, binaryName);
        fs.copyFileSync(sources[target], destination);

        if (process.platform !== 'win32') {
            fs.chmodSync(destination, 0o755);
        }

        // Trailing separator makes vsce treat --out as a directory and use its default file name. The license lives at
        // the repository root (LICENSE.md), so tell vsce not to prompt about the missing file here.
        const vsceArgs = ['package', '--target', target, '--out', outDir + path.sep, '--skip-license'];
        const result = spawnSync(process.execPath, [vsce, ...vsceArgs], {
            cwd: extensionRoot,
            stdio: 'inherit',
        });

        if (result.status !== 0) {
            fail(`vsce package failed for ${target} (exit code ${result.status}).`);
        }
    }

    console.log(`\nDone. Packages written to ${outDir}`);
}

main();
