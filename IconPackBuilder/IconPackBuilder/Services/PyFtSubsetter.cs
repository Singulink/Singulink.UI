using System.Diagnostics;
using IconPackBuilder.Core.Services;
using Singulink.IO;

namespace IconPackBuilder.Services;

public class PyFtSubsetter : IFontSubsetter
{
    private readonly string _exeName;

    public PyFtSubsetter(string exeName = "pyftsubset.exe")
    {
        _exeName = exeName ?? throw new ArgumentNullException(nameof(exeName));
    }

    public async Task SaveAsync(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destinationFile, IEnumerable<int> codePoints)
    {
        var exeFile = DirectoryPath.GetAppBase()
            .CombineDirectory("Tools")
            .CombineFile(_exeName, PathOptions.None);

        if (!exeFile.Exists)
            throw new FileNotFoundException($"'{_exeName}' not found.");

        string unicodes = string.Join("\n", codePoints.Select(cp => $"U+{cp:X}"));
        var tempFile = FilePath.CreateTempFile();

        try
        {
            await File.WriteAllTextAsync(tempFile.PathExport, unicodes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create temporary unicode list file '{tempFile.PathDisplay}': {ex.Message}", ex);
        }

        string args = $"\"{sourceFile.PathExport}\" --unicodes-file=\"{tempFile.PathExport}\" --output-file=\"{destinationFile.PathExport}\"";

        var psi = new ProcessStartInfo {
            FileName = exeFile.PathExport,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory,
        };

        await Task.Run(async () => {
            try
            {
                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                if (!process.Start())
                    throw new InvalidOperationException("Failed to start pyftsubset process.");

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync()).ConfigureAwait(false);

                int exitCode = process.ExitCode;
                string stdOut = stdOutTask.Result;
                string stdErr = stdErrTask.Result;

                if (exitCode != 0)
                {
                    string message = $"pyftsubset failed (exit code {exitCode}).\n" +
                                     $"Arguments: {args}\n" +
                                     $"STDERR:\n{stdErr}\n" +
                                     $"STDOUT:\n{stdOut}";
                    throw new InvalidOperationException(message);
                }

                if (!destinationFile.Exists)
                {
                    string message = $"pyftsubset completed successfully but the expected output file was not created: {destinationFile.PathDisplay}\n" +
                                     $"STDOUT:\n{stdOut}\n" +
                                     $"STDERR:\n{stdErr}";
                    throw new InvalidOperationException(message);
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException or FileNotFoundException or ArgumentException)
            {
                throw new InvalidOperationException("Error while running pyftsubset: " + ex.Message, ex);
            }
            finally
            {
                tempFile.Delete();
            }
        });
    }
}
