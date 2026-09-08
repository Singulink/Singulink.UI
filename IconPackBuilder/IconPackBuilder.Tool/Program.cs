using System.Diagnostics;
using System.Runtime.InteropServices;

// Launcher for the Icon Pack Builder dotnet global tool. The Uno desktop app is packaged framework-dependent under "app/" next to this assembly and
// started through the dotnet host that runs this tool, so a single package serves every platform the shared runtime supports.

string appDll = Path.Combine(AppContext.BaseDirectory, "app", "IconPackBuilder.dll");

bool wait = false;
var appArgs = new List<string>();

foreach (string arg in args)
{
    switch (arg)
    {
        case "--help" or "-h" or "-?":
            PrintUsage();
            return 0;
        case "--wait":
            wait = true;
            break;
        case "export":
            // Headless export: the app runs without a window and reports to the console, so always stay attached for its output and exit code.
            appArgs.Add(arg);
            wait = true;
            break;
        case "--register-file-association":
            return FileAssociation.Register();
        case "--unregister-file-association":
            return FileAssociation.Unregister();
        default:
            // Resolve project paths against the caller's working directory so the app is not sensitive to how it was started.
            appArgs.Add(arg.EndsWith(".ipproj", StringComparison.OrdinalIgnoreCase) ? Path.GetFullPath(arg) : arg);
            break;
    }
}

if (!File.Exists(appDll))
{
    Console.Error.WriteLine($"Icon Pack Builder app files were not found at '{appDll}'. Reinstall the tool with 'dotnet tool update -g Singulink.IconPackBuilder'.");
    return 1;
}

var psi = new ProcessStartInfo(GetDotnetHostPath()) {
    UseShellExecute = false,
    WorkingDirectory = Path.GetDirectoryName(appDll),
};

psi.ArgumentList.Add(appDll);

foreach (string arg in appArgs)
    psi.ArgumentList.Add(arg);

using var process = Process.Start(psi);

if (process is null)
{
    Console.Error.WriteLine("Failed to start Icon Pack Builder.");
    return 1;
}

if (!wait)
    return 0;

await process.WaitForExitAsync();
return process.ExitCode;

static void PrintUsage()
{
    Console.WriteLine("Usage: iconpackbuilder [options] [project.ipproj]");
    Console.WriteLine("       iconpackbuilder export <project.ipproj>");
    Console.WriteLine();
    Console.WriteLine("Opens the Singulink Icon Pack Builder, optionally loading the given project file. The export command writes the project's");
    Console.WriteLine("export folder without opening the editor (for scripts and CI) and exits with a non-zero code if the export fails.");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --wait                          Keep the console attached until the app exits and forward its exit code.");
    Console.WriteLine("  --register-file-association     Open .ipproj files with Icon Pack Builder (current user; Windows and Linux).");
    Console.WriteLine("  --unregister-file-association   Remove the .ipproj association added by --register-file-association.");
    Console.WriteLine("  -h, --help                      Show this help.");
}

static string GetDotnetHostPath()
{
    // On Windows the tool runs through an apphost shim, so the process path is not the muxer. The shared runtime directory is
    // <dotnet root>/shared/Microsoft.NETCore.App/<version>/, which locates the muxer reliably on every platform.
    string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
    string dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", ".."));
    string hostName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
    string candidate = Path.Combine(dotnetRoot, hostName);

    if (File.Exists(candidate))
        return candidate;

    if (Environment.ProcessPath is string processPath && Path.GetFileName(processPath).Equals(hostName, StringComparison.OrdinalIgnoreCase))
        return processPath;

    return "dotnet";
}
