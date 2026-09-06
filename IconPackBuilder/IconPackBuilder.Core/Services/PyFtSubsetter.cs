using System.Diagnostics;
using System.Runtime.InteropServices;
using Singulink.IO;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Subsets fonts with fonttools' <c>pyftsubset</c>. Prefers the single-file binary bundled for the current platform under
/// <c>Tools/&lt;runtime identifier&gt;</c> in the app directory, then falls back to a <c>pyftsubset</c> found on the PATH, and finally to a Python
/// installation that has fonttools installed (<c>python -m fontTools.subset</c>).
/// </summary>
public sealed class PyFtSubsetter : IFontSubsetter
{
    private static readonly string ExeName = OperatingSystem.IsWindows() ? "pyftsubset.exe" : "pyftsubset";
    private static readonly string[] PythonCandidates = OperatingSystem.IsWindows() ? ["python", "py", "python3"] : ["python3", "python"];

    private readonly IAbsoluteDirectoryPath _toolsDirectory;
    private readonly Lock _resolveLock = new();
    private Task<ToolInvocation>? _resolveTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PyFtSubsetter"/> class.
    /// </summary>
    /// <param name="toolsDirectory">The directory containing per-runtime-identifier <c>pyftsubset</c> binaries. Defaults to the <c>Tools</c>
    /// directory in the app directory.</param>
    public PyFtSubsetter(IAbsoluteDirectoryPath? toolsDirectory = null)
    {
        _toolsDirectory = toolsDirectory ?? DirectoryPath.GetAppBase().CombineDirectory("Tools");
    }

    /// <summary>
    /// Gets the path the bundled binary for the current platform is expected at.
    /// </summary>
    public IAbsoluteFilePath BundledToolFile => _toolsDirectory.CombineDirectory(RuntimeInformation.RuntimeIdentifier).CombineFile(ExeName, PathOptions.None);

    /// <inheritdoc/>
    public async Task SaveAsync(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destinationFile, IEnumerable<int> codePoints)
    {
        var tool = await ResolveToolAsync().ConfigureAwait(false);

        string unicodes = string.Join("\n", codePoints.Select(cp => $"U+{cp:X}"));
        var tempFile = FilePath.CreateTempFile();

        try
        {
            try
            {
                await File.WriteAllTextAsync(tempFile.PathExport, unicodes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create temporary unicode list file '{tempFile.PathDisplay}': {ex.Message}", ex);
            }

            var psi = new ProcessStartInfo {
                FileName = tool.FileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory,
            };

            foreach (string arg in tool.LeadingArguments)
                psi.ArgumentList.Add(arg);

            psi.ArgumentList.Add(sourceFile.PathExport);
            psi.ArgumentList.Add($"--unicodes-file={tempFile.PathExport}");
            psi.ArgumentList.Add($"--output-file={destinationFile.PathExport}");

            var (exitCode, stdOut, stdErr) = await RunAsync(psi).ConfigureAwait(false);
            string commandLine = $"{psi.FileName} {string.Join(' ', psi.ArgumentList)}";

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"pyftsubset failed (exit code {exitCode}).\nCommand: {commandLine}\nSTDERR:\n{stdErr}\nSTDOUT:\n{stdOut}");
            }

            if (!destinationFile.Exists)
            {
                throw new InvalidOperationException(
                    $"pyftsubset completed but the expected output file was not created: {destinationFile.PathDisplay}\n" +
                    $"Command: {commandLine}\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
            }
        }
        finally
        {
            tempFile.Delete();
        }
    }

    private Task<ToolInvocation> ResolveToolAsync()
    {
        lock (_resolveLock)
            return _resolveTask ??= ResolveToolCoreAsync();
    }

    private async Task<ToolInvocation> ResolveToolCoreAsync()
    {
        try
        {
            var bundled = BundledToolFile;

            if (bundled.Exists)
            {
                EnsureExecutable(bundled);
                return new ToolInvocation(bundled.PathExport, []);
            }

            if (FindOnPath(ExeName) is string onPath)
                return new ToolInvocation(onPath, []);

            foreach (string python in PythonCandidates)
            {
                if (await HasFontToolsAsync(python).ConfigureAwait(false))
                    return new ToolInvocation(python, ["-m", "fontTools.subset"]);
            }

            throw new FileNotFoundException(
                $"pyftsubset was not found. Expected the bundled binary at '{bundled.PathDisplay}', or 'pyftsubset' on the PATH, or a Python " +
                "installation with fonttools installed ('pip install fonttools').");
        }
        catch
        {
            // Do not cache failures so that installing the tool while the app is running is picked up on the next export.
            lock (_resolveLock)
                _resolveTask = null;

            throw;
        }
    }

    private static void EnsureExecutable(IAbsoluteFilePath file)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            const UnixFileMode executeBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            var mode = File.GetUnixFileMode(file.PathExport);

            if ((mode & executeBits) != executeBits)
                File.SetUnixFileMode(file.PathExport, mode | executeBits);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: starting the process will produce a clearer error if the bit really is missing.
        }
    }

    private static string? FindOnPath(string fileName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(path))
            return null;

        foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                string candidate = Path.Combine(dir, fileName);

                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException)
            {
                // Invalid PATH entry; skip it.
            }
        }

        return null;
    }

    private static async Task<bool> HasFontToolsAsync(string python)
    {
        var psi = new ProcessStartInfo {
            FileName = python,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("import fontTools.subset");

        try
        {
            var (exitCode, _, _) = await RunAsync(psi).ConfigureAwait(false);
            return exitCode is 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(ProcessStartInfo psi)
    {
        using var process = new Process { StartInfo = psi };

        if (!process.Start())
            throw new InvalidOperationException($"Failed to start '{psi.FileName}'.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync()).ConfigureAwait(false);

        return (process.ExitCode, await stdOutTask.ConfigureAwait(false), await stdErrTask.ConfigureAwait(false));
    }

    private sealed record ToolInvocation(string FileName, string[] LeadingArguments);
}
