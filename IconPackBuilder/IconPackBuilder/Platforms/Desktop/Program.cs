using IconPackBuilder.Core.IconSources;
using IconPackBuilder.Core.Services;
using Uno.UI.Hosting;

namespace IconPackBuilder;
internal sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // "export <project.ipproj>" runs the export without the editor (for scripts and CI) using the same assets and services as the UI.
        if (args.Length > 0 && args[0].Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            var exportService = new FileExportService(new PyFtSubsetter(), Exporters.All);
            return HeadlessExport.RunAsync(args[1..], SeagullIconsSource.Instance, exportService, Console.Out, Console.Error).GetAwaiter().GetResult();
        }

        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWin32()
            .UseX11()
            .UseMacOS()
            .Build();

        host.Run();
        return 0;
    }
}
