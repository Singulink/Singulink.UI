using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Opens .ipproj files from Windows Explorer for the Icon Pack Builder dotnet tool. The tool's command shim is a console program, so the file
// association runs this windowless helper, which starts the shim without a console window.
//
// Usage: IconPackBuilder.Opener.exe <path to iconpackbuilder.exe> [arguments for it]

const string Caption = "Icon Pack Builder";

if (args.Length == 0)
    return 1;

string shim = args[0];

if (!File.Exists(shim))
{
    ShowError($"Icon Pack Builder was not found at '{shim}'.\n\nInstall it with 'dotnet tool install -g Singulink.IconPackBuilder', then run " +
        "'iconpackbuilder --register-file-association' to open .ipproj files with it again.");
    return 1;
}

// CreateNoWindow gives the shim a console without a window, which the app it starts inherits.
var psi = new ProcessStartInfo(shim) { UseShellExecute = false, CreateNoWindow = true };

foreach (string arg in args.Skip(1))
    psi.ArgumentList.Add(arg);

try
{
    using var process = Process.Start(psi);
    return process is null ? 1 : 0;
}
catch (Win32Exception ex)
{
    ShowError($"Icon Pack Builder could not be started: {ex.Message}");
    return 1;
}

static void ShowError(string message) => _ = MessageBoxW(IntPtr.Zero, message, Caption, 0x10 /* MB_ICONERROR */);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
