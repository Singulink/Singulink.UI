using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Per-user ".ipproj" file association pointing at this tool's launcher. Global tools cannot register anything at install time, so this is an explicit
/// command. Windows uses the current user's registry classes; Linux uses a freedesktop MIME type and .desktop entry. macOS associations require an
/// application bundle, which a dotnet tool does not have, so it is not supported.
/// </summary>
internal static class FileAssociation
{
    private const string Extension = ".ipproj";
    private const string ProgId = "Singulink.IconPackBuilder.Project";
    private const string MimeType = "application/x-icon-pack-builder-project";
    private const string DesktopEntryName = "singulink-icon-pack-builder.desktop";

    public static int Register()
    {
        string launcher = GetLauncherPath();

        if (OperatingSystem.IsWindows())
        {
            RegisterWindows(launcher);
            Console.WriteLine($"Associated {Extension} files with Icon Pack Builder for the current user ({launcher}).");
            return 0;
        }

        if (OperatingSystem.IsLinux())
        {
            RegisterLinux(launcher);
            Console.WriteLine($"Associated {Extension} files with Icon Pack Builder for the current user ({launcher}).");
            return 0;
        }

        Console.Error.WriteLine("File associations are only supported on Windows and Linux. On macOS, open projects with 'iconpackbuilder <file>'.");
        return 1;
    }

    public static int Unregister()
    {
        if (OperatingSystem.IsWindows())
        {
            UnregisterWindows();
            Console.WriteLine($"Removed the {Extension} association for the current user.");
            return 0;
        }

        if (OperatingSystem.IsLinux())
        {
            UnregisterLinux();
            Console.WriteLine($"Removed the {Extension} association for the current user.");
            return 0;
        }

        Console.Error.WriteLine("File associations are only supported on Windows and Linux.");
        return 1;
    }

    private static string GetLauncherPath()
    {
        // The apphost shim (iconpackbuilder[.exe]) that 'dotnet tool install' created is the stable entry point; the tool's own files move on update.
        string? processPath = Environment.ProcessPath;

        if (processPath is not null && Path.GetFileNameWithoutExtension(processPath).Equals("iconpackbuilder", StringComparison.OrdinalIgnoreCase))
            return processPath;

        string toolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        return Path.Combine(toolsDir, OperatingSystem.IsWindows() ? "iconpackbuilder.exe" : "iconpackbuilder");
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterWindows(string launcher)
    {
        using var classes = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes");

        using (var ext = classes.CreateSubKey(Extension))
        {
            ext.SetValue(null, ProgId);
            ext.SetValue("Content Type", "application/json");
            ext.SetValue("PerceivedType", "text");
        }

        using var progId = classes.CreateSubKey(ProgId);
        progId.SetValue(null, "Icon Pack Builder Project");

        using (var icon = progId.CreateSubKey("DefaultIcon"))
            icon.SetValue(null, $"\"{launcher}\",0");

        using var command = progId.CreateSubKey(@"shell\open\command");
        command.SetValue(null, $"\"{launcher}\" \"%1\"");

        NotifyWindowsShell();
    }

    [SupportedOSPlatform("windows")]
    private static void UnregisterWindows()
    {
        using var classes = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", writable: true);

        if (classes is null)
            return;

        using (var ext = classes.OpenSubKey(Extension, writable: true))
        {
            if (ext is not null && string.Equals(ext.GetValue(null) as string, ProgId, StringComparison.Ordinal))
                classes.DeleteSubKeyTree(Extension, throwOnMissingSubKey: false);
        }

        classes.DeleteSubKeyTree(ProgId, throwOnMissingSubKey: false);
        NotifyWindowsShell();
    }

    [SupportedOSPlatform("windows")]
    private static void NotifyWindowsShell()
    {
        // SHCNE_ASSOCCHANGED so Explorer picks up the new association without a sign-out.
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    private static void RegisterLinux(string launcher)
    {
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        string mimeDir = Path.Combine(dataHome, "mime", "packages");
        string appsDir = Path.Combine(dataHome, "applications");
        Directory.CreateDirectory(mimeDir);
        Directory.CreateDirectory(appsDir);

        string mimeXml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
              <mime-type type="{MimeType}">
                <comment>Icon Pack Builder project</comment>
                <sub-class-of type="application/json"/>
                <glob pattern="*{Extension}"/>
              </mime-type>
            </mime-info>

            """;

        File.WriteAllText(Path.Combine(mimeDir, "singulink-icon-pack-builder.xml"), mimeXml);

        string desktopEntry = $"""
            [Desktop Entry]
            Type=Application
            Name=Icon Pack Builder
            Comment=Build trimmed font icon packs
            Exec="{launcher}" %f
            Terminal=false
            Categories=Development;
            MimeType={MimeType};

            """;

        File.WriteAllText(Path.Combine(appsDir, DesktopEntryName), desktopEntry);

        RunQuietly("update-mime-database", Path.Combine(dataHome, "mime"));
        RunQuietly("update-desktop-database", appsDir);
        RunQuietly("xdg-mime", "default", DesktopEntryName, MimeType);
    }

    private static void UnregisterLinux()
    {
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        File.Delete(Path.Combine(dataHome, "mime", "packages", "singulink-icon-pack-builder.xml"));
        File.Delete(Path.Combine(dataHome, "applications", DesktopEntryName));

        RunQuietly("update-mime-database", Path.Combine(dataHome, "mime"));
        RunQuietly("update-desktop-database", Path.Combine(dataHome, "applications"));
    }

    private static void RunQuietly(string fileName, params string[] arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

            foreach (string argument in arguments)
                psi.ArgumentList.Add(argument);

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            // Optional desktop integration tools; the files written above are still honored once the databases are refreshed.
        }
    }
}
