using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class HeadlessExportTests
{
    [TestMethod]
    public async Task Run_ExportsProjectWithoutEditor()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, exports: [("ArrowLeft", "Back", ["Regular"]), ("Save", string.Empty, ["Filled"])]);

        var subsetter = new FakeFontSubsetter();
        var exporter = new RecordingExporter(ExportFormat.CSharp);
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(subsetter, [exporter]), output, error);

        exitCode.ShouldBe(0, error.ToString());
        error.ToString().ShouldBeEmpty();
        output.ToString().ShouldContain("Exported 2 icon(s)");
        output.ToString().ShouldContain("Test.Icons_Export");

        var arrowLeft = FakeIconsSource.Instance.LoadIconGroups().First(g => g.Id == "ArrowLeft").Icons[0];
        var saveFilled = FakeIconsSource.Instance.LoadIconGroups().First(g => g.Id == "Save").Icons[1];
        var subset = subsetter.Calls.ShouldHaveSingleItem();
        subset.CodePoints.ShouldBe([arrowLeft.CodePoint, arrowLeft.RtlCodePoint!.Value, saveFilled.CodePoint], ignoreOrder: true);
        subset.Destination.PathDisplay.ShouldEndWith(Path.Combine("Test.Icons_Export", "Test.Icons.otf"));

        var context = exporter.Context.ShouldNotBeNull();
        context.ProjectName.ShouldBe("Test.Icons");
        context.DefaultVariantName.ShouldBe("Regular");
        context.Icons.Select(i => (i.ExportName, i.Icon.Variant)).ShouldBe([("Back", "Regular"), ("Save", "Filled")], ignoreOrder: true);
    }

    [TestMethod]
    public async Task Run_OnlyRunsEnabledFormats()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, formats: [ExportFormat.Css], exports: [("Add", string.Empty, ["Regular"])]);

        var csharp = new RecordingExporter(ExportFormat.CSharp);
        var css = new RecordingExporter(ExportFormat.Css);
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(new FakeFontSubsetter(), [csharp, css]), output, error);

        exitCode.ShouldBe(0, error.ToString());
        csharp.Context.ShouldBeNull();
        css.Context.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Run_MissingIcon_FailsInsteadOfExportingPartialPack()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, exports: [("Add", string.Empty, ["Regular"]), ("Gone", string.Empty, ["Regular"])]);

        var subsetter = new FakeFontSubsetter();
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(subsetter, []), output, error);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("'Gone' is missing");
        subsetter.Calls.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Run_MissingVariant_Fails()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, exports: [("Add", string.Empty, ["Regular", "Light"])]);
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(new FakeFontSubsetter(), []), output, error);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("no 'Light' variant");
    }

    [TestMethod]
    public async Task Run_NewerSourceVersion_WarnsButExports()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, sourceVersion: new Version(9, 0), exports: [("Add", string.Empty, ["Regular"])]);
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(new FakeFontSubsetter(), []), output, error);

        exitCode.ShouldBe(0);
        error.ToString().ShouldContain("warning:");
        error.ToString().ShouldContain("9.0");
    }

    [TestMethod]
    public async Task Run_InvalidArguments_ReturnUsage()
    {
        var service = new FileExportService(new FakeFontSubsetter(), []);
        var (output, error) = (new StringWriter(), new StringWriter());

        (await HeadlessExport.RunAsync([], FakeIconsSource.Instance, service, output, error)).ShouldBe(2);
        (await HeadlessExport.RunAsync(["--help"], FakeIconsSource.Instance, service, output, error)).ShouldBe(2);
        (await HeadlessExport.RunAsync(["a.ipproj", "b.ipproj"], FakeIconsSource.Instance, service, output, error)).ShouldBe(2);
        error.ToString().ShouldContain(HeadlessExport.Usage);

        string missing = Path.Combine(TestFiles.NewTempDirectory(), "Missing.ipproj");
        (await HeadlessExport.RunAsync([missing], FakeIconsSource.Instance, service, output, error)).ShouldBe(1);
        error.ToString().ShouldContain("not found");
    }

    [TestMethod]
    public async Task Run_WrongIconSource_Fails()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, sourceId: "SomethingElse", exports: [("Add", string.Empty, ["Regular"])]);
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(new FakeFontSubsetter(), []), output, error);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("SomethingElse");
    }

    [TestMethod]
    public async Task Run_MalformedProject_Fails()
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        File.WriteAllText(path, "{ not json");
        var (output, error) = (new StringWriter(), new StringWriter());

        int exitCode = await HeadlessExport.RunAsync([path], FakeIconsSource.Instance, new FileExportService(new FakeFontSubsetter(), []), output, error);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("error:");
    }
}

/// <summary>
/// Exporter that records the context it was run with.
/// </summary>
public sealed class RecordingExporter(ExportFormat format = ExportFormat.CSharp) : IExporter
{
    public string Name => "Recording";

    public ExportFormat Format => format;

    public ExportContext? Context { get; private set; }

    public Task SaveAsync(ExportContext context)
    {
        Context = context;
        return Task.CompletedTask;
    }
}
