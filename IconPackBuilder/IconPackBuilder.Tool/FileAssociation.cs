using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Per-user ".ipproj" file association pointing at this tool's launcher. Global tools cannot register anything at install time, so this is an explicit
/// command. Windows uses the current user's registry classes; Linux uses a freedesktop MIME type and .desktop entry. macOS associations require an
/// application bundle, which a dotnet tool does not have, so it is not supported.
/// </summary>
/// <remarks>
/// The launcher (the apphost shim that 'dotnet tool install' created) is a console program, so on Windows the association runs the windowless
/// IconPackBuilder.Opener helper, which starts the launcher without a console window. The helper and the icon are copied out of the package into a
/// stable per-user folder, because the tool's own files move to a new folder on every update while the shim stays where it is.
/// </remarks>
internal static class FileAssociation
{
    private const string Extension = ".ipproj";
    private const string ProgId = "Singulink.IconPackBuilder.Project";
    private const string MimeType = "application/x-icon-pack-builder-project";
    private const string DesktopEntryName = "singulink-icon-pack-builder.desktop";
    private const string IconName = "singulink-icon-pack-builder";
    private const string OpenerFileName = "IconPackBuilder.Opener.exe";

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

    /// <summary>
    /// Brings an existing association up to date with this version of the tool (for example one registered by an older version that ran the console
    /// launcher directly). Does nothing when no association is registered or when this process was not started through the installed launcher.
    /// </summary>
    public static void RefreshIfRegistered()
    {
        try
        {
            if (!IsRunningFromLauncher(out string? launcher))
                return;

            if (OperatingSystem.IsWindows())
            {
                using var progIdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + ProgId);

                if (progIdKey is not null)
                    RegisterWindows(launcher);
            }
            else if (OperatingSystem.IsLinux())
            {
                if (File.Exists(Path.Combine(GetLinuxDataHome(), "applications", DesktopEntryName)))
                    RegisterLinux(launcher);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Keeping the association current is a convenience; never let it stop the app from starting.
        }
    }

    private static string GetLauncherPath()
    {
        // The apphost shim (iconpackbuilder[.exe]) that 'dotnet tool install' created is the stable entry point; the tool's own files move on update.
        if (IsRunningFromLauncher(out string? launcher))
            return launcher;

        string toolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        return Path.Combine(toolsDir, OperatingSystem.IsWindows() ? "iconpackbuilder.exe" : "iconpackbuilder");
    }

    private static bool IsRunningFromLauncher([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? launcher)
    {
        launcher = Environment.ProcessPath;
        return launcher is not null && Path.GetFileNameWithoutExtension(launcher).Equals("iconpackbuilder", StringComparison.OrdinalIgnoreCase);
    }

    // Its own subfolder: the app keeps its data (recent projects) in the parent folder, and unregistering deletes this one.
    [SupportedOSPlatform("windows")]
    private static string GetWindowsSupportDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Singulink", "IconPackBuilder", "FileAssociation");

    [SupportedOSPlatform("windows")]
    private static void RegisterWindows(string launcher)
    {
        bool changed = false;
        string supportDir = GetWindowsSupportDirectory();

        // The opener matching this machine's architecture (an x64 apphost on Arm64 would look for an x64 .NET runtime).
        string rid = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        string openerSource = Path.Combine(AppContext.BaseDirectory, "opener", rid);
        string openerDir = Path.Combine(supportDir, "Opener");
        string? opener = null;

        if (File.Exists(Path.Combine(openerSource, OpenerFileName)))
        {
            changed |= CopyDirectoryIfDifferent(openerSource, openerDir);
            opener = Path.Combine(openerDir, OpenerFileName);
        }

        string iconSource = Path.Combine(AppContext.BaseDirectory, "icons", "IconPackBuilder.ico");
        string icon = Path.Combine(supportDir, "IconPackBuilder.ico");

        if (File.Exists(iconSource))
            changed |= CopyFileIfDifferent(iconSource, icon);
        else
            icon = opener ?? launcher;

        using var classes = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes");

        using (var ext = classes.CreateSubKey(Extension))
        {
            changed |= SetIfDifferent(ext, null, ProgId);
            changed |= SetIfDifferent(ext, "Content Type", "application/json");
            changed |= SetIfDifferent(ext, "PerceivedType", "text");
        }

        using var progId = classes.CreateSubKey(ProgId);
        changed |= SetIfDifferent(progId, null, "Icon Pack Builder Project");

        using (var defaultIcon = progId.CreateSubKey("DefaultIcon"))
            changed |= SetIfDifferent(defaultIcon, null, $"\"{icon}\",0");

        // Without the opener (a development build run from its output folder) the launcher is run directly, console window and all.
        string command = opener is not null ? $"\"{opener}\" \"{launcher}\" \"%1\"" : $"\"{launcher}\" \"%1\"";

        using (var commandKey = progId.CreateSubKey(@"shell\open\command"))
            changed |= SetIfDifferent(commandKey, null, command);

        if (changed)
            NotifyWindowsShell();
    }

    [SupportedOSPlatform("windows")]
    private static void UnregisterWindows()
    {
        using (var classes = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", writable: true))
        {
            if (classes is not null)
            {
                using (var ext = classes.OpenSubKey(Extension, writable: true))
                {
                    if (ext is not null && string.Equals(ext.GetValue(null) as string, ProgId, StringComparison.Ordinal))
                        classes.DeleteSubKeyTree(Extension, throwOnMissingSubKey: false);
                }

                classes.DeleteSubKeyTree(ProgId, throwOnMissingSubKey: false);
            }
        }

        try
        {
            Directory.Delete(GetWindowsSupportDirectory(), recursive: true);
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            // Already gone, or the opener is running right now; the files are harmless without the registry entries.
        }

        NotifyWindowsShell();
    }

    [SupportedOSPlatform("windows")]
    private static bool SetIfDifferent(Microsoft.Win32.RegistryKey key, string? name, string value)
    {
        if (string.Equals(key.GetValue(name) as string, value, StringComparison.Ordinal))
            return false;

        key.SetValue(name, value);
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static void NotifyWindowsShell()
    {
        // SHCNE_ASSOCCHANGED so Explorer picks up the new association (and icon) without a sign-out.
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    private static string GetLinuxDataHome() => Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
        ? xdg
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

    private static void RegisterLinux(string launcher)
    {
        string dataHome = GetLinuxDataHome();
        string mimeDir = Path.Combine(dataHome, "mime", "packages");
        string appsDir = Path.Combine(dataHome, "applications");
        string iconDir = Path.Combine(dataHome, "icons", "hicolor", "256x256", "apps");
        Directory.CreateDirectory(mimeDir);
        Directory.CreateDirectory(appsDir);

        bool iconChanged = false;
        string iconSource = Path.Combine(AppContext.BaseDirectory, "icons", "IconPackBuilder.png");
        bool hasIcon = File.Exists(iconSource);

        if (hasIcon)
        {
            Directory.CreateDirectory(iconDir);
            iconChanged = CopyFileIfDifferent(iconSource, Path.Combine(iconDir, IconName + ".png"));
        }

        string iconElement = hasIcon ? $"""{"\n"}    <icon name="{IconName}"/>""" : string.Empty;

        string mimeXml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
              <mime-type type="{MimeType}">
                <comment>Icon Pack Builder project</comment>
                <sub-class-of type="application/json"/>
                <glob pattern="*{Extension}"/>{iconElement}
              </mime-type>
            </mime-info>

            """;

        bool mimeChanged = WriteFileIfDifferent(Path.Combine(mimeDir, "singulink-icon-pack-builder.xml"), mimeXml);

        string iconLine = hasIcon ? $"Icon={IconName}\n" : string.Empty;

        string desktopEntry = $"""
            [Desktop Entry]
            Type=Application
            Name=Icon Pack Builder
            Comment=Build trimmed font icon packs
            Exec="{launcher}" %f
            {iconLine}Terminal=false
            Categories=Development;
            MimeType={MimeType};

            """;

        bool desktopChanged = WriteFileIfDifferent(Path.Combine(appsDir, DesktopEntryName), desktopEntry);

        if (mimeChanged)
            RunQuietly("update-mime-database", Path.Combine(dataHome, "mime"));

        if (iconChanged)
            RunQuietly("gtk-update-icon-cache", "--force", "--ignore-theme-index", Path.Combine(dataHome, "icons", "hicolor"));

        if (desktopChanged)
        {
            RunQuietly("update-desktop-database", appsDir);
            RunQuietly("xdg-mime", "default", DesktopEntryName, MimeType);
        }
    }

    private static void UnregisterLinux()
    {
        string dataHome = GetLinuxDataHome();

        File.Delete(Path.Combine(dataHome, "mime", "packages", "singulink-icon-pack-builder.xml"));
        File.Delete(Path.Combine(dataHome, "applications", DesktopEntryName));
        File.Delete(Path.Combine(dataHome, "icons", "hicolor", "256x256", "apps", IconName + ".png"));

        RunQuietly("update-mime-database", Path.Combine(dataHome, "mime"));
        RunQuietly("update-desktop-database", Path.Combine(dataHome, "applications"));
        RunQuietly("gtk-update-icon-cache", "--force", "--ignore-theme-index", Path.Combine(dataHome, "icons", "hicolor"));
    }

    private static bool CopyDirectoryIfDifferent(string sourceDir, string targetDir)
    {
        bool changed = false;
        Directory.CreateDirectory(targetDir);

        foreach (string source in Directory.EnumerateFiles(sourceDir))
            changed |= CopyFileIfDifferent(source, Path.Combine(targetDir, Path.GetFileName(source)));

        return changed;
    }

    private static bool CopyFileIfDifferent(string source, string target)
    {
        if (File.Exists(target) && File.ReadAllBytes(source).AsSpan().SequenceEqual(File.ReadAllBytes(target)))
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        return true;
    }

    private static bool WriteFileIfDifferent(string path, string contents)
    {
        if (File.Exists(path) && string.Equals(File.ReadAllText(path), contents, StringComparison.Ordinal))
            return false;

        File.WriteAllText(path, contents);
        return true;
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
