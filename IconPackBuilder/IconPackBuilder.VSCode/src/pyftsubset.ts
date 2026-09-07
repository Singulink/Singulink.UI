import { spawn } from 'node:child_process';
import * as fs from 'node:fs/promises';
import * as path from 'node:path';

/** A way of invoking pyftsubset: the executable plus any arguments that precede the pyftsubset arguments. */
interface ToolInvocation {
    file: string;
    leadingArgs: string[];
    description: string;
}

/** Invocation that worked last time, so subsequent exports skip the probing. */
let cachedInvocation: ToolInvocation | undefined;

/**
 * Runs `pyftsubset` with the given arguments, resolving the tool in this order:
 *
 * 1. The binary bundled with the extension (`<extension>/bin/pyftsubset[.exe]`).
 * 2. `pyftsubset` on the PATH.
 * 3. `python3 -m fontTools.subset`, then `python -m fontTools.subset`.
 *
 * Rejects with an Error whose message includes the tool's stderr if subsetting fails, or a message explaining how to
 * install fonttools if no tool could be found.
 */
export async function runPyftsubset(extensionPath: string, args: string[]): Promise<void> {
    if (cachedInvocation) {
        const result = await execute(cachedInvocation, args);

        if (result.kind === 'success') {
            return;
        }

        if (result.kind === 'failed') {
            throw new Error(result.error);
        }

        // The cached tool has disappeared (e.g. uninstalled); fall through to probing again.
        cachedInvocation = undefined;
    }

    const unavailable: string[] = [];

    for (const invocation of await getCandidates(extensionPath)) {
        const result = await execute(invocation, args);

        switch (result.kind) {
            case 'success':
                cachedInvocation = invocation;
                return;

            case 'failed':
                // The tool ran but subsetting failed. Do not try other tools; they would fail the same way.
                throw new Error(result.error);

            case 'unavailable':
                unavailable.push(`${invocation.description}: ${result.reason}`);
                break;
        }
    }

    throw new Error(
        'pyftsubset could not be found. Install fonttools (`pip install fonttools`) or make `pyftsubset` available ' +
        'on the PATH.\n' + unavailable.join('\n'),
    );
}

async function getCandidates(extensionPath: string): Promise<ToolInvocation[]> {
    const candidates: ToolInvocation[] = [];
    const bundled = await getBundledBinary(extensionPath);

    if (bundled) {
        candidates.push({ file: bundled, leadingArgs: [], description: 'bundled binary' });
    }

    candidates.push({ file: 'pyftsubset', leadingArgs: [], description: 'pyftsubset on PATH' });
    candidates.push({ file: 'python3', leadingArgs: ['-m', 'fontTools.subset'], description: 'python3 -m fontTools.subset' });
    candidates.push({ file: 'python', leadingArgs: ['-m', 'fontTools.subset'], description: 'python -m fontTools.subset' });

    return candidates;
}

/** Returns the path of the bundled binary if it exists (and is executable on POSIX), otherwise undefined. */
async function getBundledBinary(extensionPath: string): Promise<string | undefined> {
    const isWindows = process.platform === 'win32';
    const binary = path.join(extensionPath, 'bin', isWindows ? 'pyftsubset.exe' : 'pyftsubset');

    try {
        await fs.access(binary);
    }
    catch {
        return undefined;
    }

    if (!isWindows) {
        // VSIX extraction does not preserve POSIX permissions, so make sure the binary is executable.
        try {
            await fs.access(binary, fs.constants.X_OK);
        }
        catch {
            try {
                await fs.chmod(binary, 0o755);
            }
            catch (err) {
                console.warn(`[IconPackBuilder] Could not make ${binary} executable:`, err);
                return undefined;
            }
        }
    }

    return binary;
}

type ExecuteResult =
    | { kind: 'success' }
    | { kind: 'failed'; error: string }
    | { kind: 'unavailable'; reason: string };

/**
 * Runs an invocation and classifies the outcome. "unavailable" means the tool itself is missing (so the next
 * candidate should be tried), while "failed" means the tool ran and reported an error.
 */
function execute(invocation: ToolInvocation, args: string[]): Promise<ExecuteResult> {
    return new Promise<ExecuteResult>(resolve => {
        const child = spawn(invocation.file, [...invocation.leadingArgs, ...args], {
            windowsHide: true,
            stdio: ['ignore', 'pipe', 'pipe'],
        });

        let stdout = '';
        let stderr = '';
        let settled = false;

        const settle = (result: ExecuteResult) => {
            if (!settled) {
                settled = true;
                resolve(result);
            }
        };

        child.stdout.setEncoding('utf8').on('data', chunk => { stdout += chunk; });
        child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });

        child.on('error', err => {
            const code = (err as NodeJS.ErrnoException).code;
            settle(code === 'ENOENT' || code === 'EACCES'
                ? { kind: 'unavailable', reason: `not found (${code})` }
                : { kind: 'failed', error: `${invocation.description} could not be started: ${err.message}` });
        });

        child.on('close', (exitCode, signal) => {
            if (exitCode === 0) {
                settle({ kind: 'success' });
                return;
            }

            const output = (stderr || stdout).trim();

            if (isMissingModuleOutput(invocation, exitCode, output)) {
                settle({ kind: 'unavailable', reason: 'fontTools module not installed' });
                return;
            }

            const status = exitCode === null ? `signal ${signal}` : `exit code ${exitCode}`;
            const detail = output ? `\n${truncate(output, 2000)}` : '';
            settle({ kind: 'failed', error: `${invocation.description} failed (${status}).${detail}` });
        });
    });
}

/**
 * Detects the "tool is not really there" outputs of the Python fallbacks: fontTools not installed, or the Windows
 * Store `python` alias stub that prints an install hint instead of running Python (exit code 9009).
 */
function isMissingModuleOutput(invocation: ToolInvocation, exitCode: number | null, output: string): boolean {
    if (invocation.leadingArgs.length === 0) {
        return false;
    }

    return exitCode === 9009
        || /No module named/i.test(output)
        || /Python was not found/i.test(output);
}

function truncate(text: string, maxLength: number): string {
    return text.length <= maxLength ? text : text.slice(0, maxLength) + '...';
}
