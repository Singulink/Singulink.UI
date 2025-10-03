using Uno.UI.Hosting;

namespace IconPackBuilder;
internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        // Disabled platforms:
        // UseX11()
        // UseLinuxFrameBuffer()
        // UseMacOS()

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWin32()
            .Build();

        host.Run();
    }
}
