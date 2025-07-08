using Uno.UI.Hosting;

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Playground.iOS;
#pragma warning restore SA1300

public static class Program
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
