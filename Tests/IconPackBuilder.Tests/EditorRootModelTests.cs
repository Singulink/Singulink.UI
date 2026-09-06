using IconPackBuilder.ViewModels;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.Testing;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class EditorRootModelTests
{
    [TestMethod]
    public void Load_AppliesExports_RecordsRecentProject_AndDoesNotLockTheFile()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, services, path) = await OpenEditorAsync(("Add", string.Empty, ["Regular"]), ("Alert", "Notifications", ["Regular", "Filled"]));
            var editor = nav.ActiveViewModel<EditorRootModel>();

            editor.ProjectName.ShouldBe("Test.Icons");
            editor.IsDirty.ShouldBeFalse();
            Icon(editor, "Add", "Regular").IsSelected.ShouldBeTrue();
            Icon(editor, "Add", "Filled").IsSelected.ShouldBeFalse();
            Icon(editor, "Alert", "Filled").IsSelected.ShouldBeTrue();
            Group(editor, "Alert").ExportName.ShouldBe("Notifications");
            Group(editor, "Alert").FinalExportName.ShouldBe("Notifications");
            Group(editor, "Add").FinalExportName.ShouldBe("Add");

            services.RecentProjects.Projects.Single().Path.ShouldBe(path);
            services.RecentProjects.Projects.Single().Name.ShouldBe("Test.Icons");

            // Other programs must be able to open the file exclusively while it is being edited.
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        });
    }

    [TestMethod]
    public void ExternalChange_ReloadsWhenClean_PromptsWhenDirty()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, path) = await OpenEditorAsync(("Add", string.Empty, ["Regular"]));
            var editor = nav.ActiveViewModel<EditorRootModel>();
            int prompts = 0;
            int answer = 1;
            nav.OnMessageDialog(m =>
            {
                prompts++;
                return answer;
            });

            // Clean: silent reload replaces the whole selection.
            TestFiles.WriteProject(path, exports: [("Alert", "Notifications", ["Filled"])]);
            await SettleAsync(nav);

            Icon(editor, "Add", "Regular").IsSelected.ShouldBeFalse();
            Icon(editor, "Alert", "Filled").IsSelected.ShouldBeTrue();
            Group(editor, "Alert").ExportName.ShouldBe("Notifications");
            editor.IsDirty.ShouldBeFalse();
            prompts.ShouldBe(0);

            // Dirty, keep my changes: nothing is reloaded and the editor stays dirty.
            Icon(editor, "Save", "Regular").IsSelected = true;
            editor.IsDirty.ShouldBeTrue();
            answer = 1;
            TestFiles.WriteProject(path, exports: [("Add", string.Empty, ["Regular"])]);
            await SettleAsync(nav);

            prompts.ShouldBe(1);
            Icon(editor, "Save", "Regular").IsSelected.ShouldBeTrue();
            Icon(editor, "Alert", "Filled").IsSelected.ShouldBeTrue();
            editor.IsDirty.ShouldBeTrue();

            // Dirty, reload: the file wins.
            answer = 0;
            TestFiles.WriteProject(path, exports: [("Add", "Plus", ["Regular", "Filled"])]);
            await SettleAsync(nav);

            prompts.ShouldBe(2);
            Icon(editor, "Add", "Filled").IsSelected.ShouldBeTrue();
            Icon(editor, "Save", "Regular").IsSelected.ShouldBeFalse();
            Group(editor, "Add").ExportName.ShouldBe("Plus");
            editor.IsDirty.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void Save_WritesAtomically_AndIsNotTreatedAsAnExternalChange()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, path) = await OpenEditorAsync(("Add", string.Empty, ["Regular"]));
            var editor = nav.ActiveViewModel<EditorRootModel>();
            int prompts = 0;
            nav.OnMessageDialog(m =>
            {
                prompts++;
                return 0;
            });

            Icon(editor, "Save", "Filled").IsSelected = true;
            Group(editor, "Save").ExportName = "Store";
            await editor.SaveProjectCommand.ExecuteAsync(null);

            editor.IsDirty.ShouldBeFalse();
            await SettleAsync(nav);
            prompts.ShouldBe(0);
            Icon(editor, "Save", "Filled").IsSelected.ShouldBeTrue();
            File.Exists(path + ".tmp").ShouldBeFalse();

            var saved = TestFiles.ReadProject(path);
            saved.IconsSourceVersion.ShouldBe(FakeIconsSource.Instance.Version);
            saved.IconExports.Select(e => (e.GroupId, e.ExportName, string.Join(',', e.Variants)))
                .ShouldBe([("Add", string.Empty, "Regular"), ("Save", "Store", "Filled")], ignoreOrder: true);
        });
    }

    [TestMethod]
    public void Close_StopsWatchingTheFile()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, path) = await OpenEditorAsync(("Add", string.Empty, ["Regular"]));
            int prompts = 0;
            nav.OnMessageDialog(m =>
            {
                prompts++;
                return 0;
            });

            await nav.NavigateAsync(Routes.StartRoot);
            nav.ActiveViewModel<StartRootModel>().ShouldNotBeNull();

            TestFiles.WriteProject(path, exports: [("Alert", string.Empty, ["Regular"])]);
            await SettleAsync(nav);
            prompts.ShouldBe(0);
        });
    }

    [TestMethod]
    public void Filter_RanksNameMatchesAboveKeywordMatches()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, _) = await OpenEditorAsync();
            var editor = nav.ActiveViewModel<EditorRootModel>();

            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["Add", "Alert", "ArrowLeft", "Save"]);
            Group(editor, "Save").ExportName = "Archive";

            // "back" is a keyword of Arrow Left only; "al" prefix-matches the Alert name and nothing else by keyword.
            editor.NameFilter = "back";
            await SettleAsync(nav);
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["ArrowLeft"]);

            editor.NameFilter = "al";
            await SettleAsync(nav);
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["Alert"]);

            // "a" matches Add, Alert and Arrow Left by name, and Save only through its export name.
            editor.NameFilter = "a";
            await SettleAsync(nav);
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["Add", "Alert", "ArrowLeft", "Save"]);

            // Multiple terms must all match; the name match ("arrow") ranks the group even though "previous" is only a keyword.
            editor.NameFilter = "arrow previous";
            await SettleAsync(nav);
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["ArrowLeft"]);

            // "store" is only a Save keyword.
            editor.NameFilter = "store";
            await SettleAsync(nav);
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["Save"]);

            editor.NameFilter = string.Empty;
            await SettleAsync(nav);
            editor.FilteredIconGroups.Count.ShouldBe(4);
        });
    }

    [TestMethod]
    public void Filters_RtlOnly_IncludedOnly_AndVariant()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, _) = await OpenEditorAsync(("Add", string.Empty, ["Filled"]));
            var editor = nav.ActiveViewModel<EditorRootModel>();

            editor.RtlVersionsOnlyFilter = true;
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["ArrowLeft"]);
            editor.RtlVersionsOnlyFilter = false;

            editor.IncludedOnlyFilter = true;
            editor.FilteredIconGroups.Select(g => g.Info.Id).ShouldBe(["Add"]);
            editor.IncludedOnlyFilter = false;

            editor.VariantFilter = "Filled";
            editor.FilteredIconGroups.ShouldAllBe(g => g.ActiveIconInfo!.Variant == "Filled");
            editor.VariantFilter = "All";
            editor.FilteredIconGroups.ShouldAllBe(g => g.ActiveIconInfo!.Variant == "Regular");
        });
    }

    [TestMethod]
    public void Selection_TracksSelectedGroup()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _, _) = await OpenEditorAsync();
            var editor = nav.ActiveViewModel<EditorRootModel>();

            var first = editor.FilteredIconGroups[0];
            var second = editor.FilteredIconGroups[1];
            editor.SelectedIconGroup.ShouldBeSameAs(first);
            first.IsSelected.ShouldBeTrue();

            editor.SelectedIconGroup = second;
            first.IsSelected.ShouldBeFalse();
            second.IsSelected.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void Load_WarnsAboutMissingIconsAndSourceDowngrade()
    {
        NavigationTestContext.Run(async () =>
        {
            string dir = TestFiles.NewTempDirectory();
            string path = Path.Combine(dir, "Test.Icons.ipproj");
            TestFiles.WriteProject(path, sourceVersion: new Version(9, 9), exports: [("Add", string.Empty, ["Regular"]), ("Gone", string.Empty, ["Regular"]), ("Alert", string.Empty, ["Light"])]);

            var messages = new List<string>();
            var nav = BuildNav(new TestServices());
            nav.OnMessageDialog(m =>
            {
                messages.Add(m.Message);
                return 0;
            });

            (await nav.NavigateAsync(Routes.EditorRoot.ToConcrete(path))).ShouldBe(NavigationResult.Success);

            var editor = nav.ActiveViewModel<EditorRootModel>();
            Icon(editor, "Add", "Regular").IsSelected.ShouldBeTrue();
            editor.IsDirty.ShouldBeFalse();

            string warning = messages.ShouldHaveSingleItem();
            warning.ShouldContain("Downgrade");
            warning.ShouldContain("'Gone'");
            warning.ShouldContain("'Light'");
        });
    }

    [TestMethod]
    public void Load_WrongSource_RedirectsToStart()
    {
        NavigationTestContext.Run(async () =>
        {
            string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
            TestFiles.WriteProject(path);
            string json = File.ReadAllText(path).Replace(FakeIconsSource.SourceId, "SomethingElse");
            File.WriteAllText(path, json);

            var messages = new List<string>();
            var nav = BuildNav(new TestServices());
            nav.OnMessageDialog(m =>
            {
                messages.Add(m.Message);
                return 0;
            });

            await nav.NavigateAsync(Routes.EditorRoot.ToConcrete(path));

            nav.ActiveViewModel<StartRootModel>().ShouldNotBeNull();
            messages.ShouldHaveSingleItem().ShouldContain("SomethingElse");
        });
    }

    [TestMethod]
    public void Export_SubsetsSelectedCodePointsIncludingRtl_AndRunsExporters()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            var exporter = new RecordingExporter();
            services.Exporters.Add(exporter);
            var (nav, _, path) = await OpenEditorAsync(services, ("ArrowLeft", "Back", ["Regular"]), ("Save", string.Empty, ["Filled"]));
            var editor = nav.ActiveViewModel<EditorRootModel>();
            var messages = new List<string>();
            nav.OnMessageDialog(m =>
            {
                messages.Add(m.Message);
                return 0;
            });

            await editor.ExportProjectCommand.ExecuteAsync(null);

            messages.ShouldHaveSingleItem().ShouldContain("exported successfully");

            var subset = services.FontSubsetter.Calls.ShouldHaveSingleItem();
            var arrowLeft = Icon(editor, "ArrowLeft", "Regular").Info;
            subset.CodePoints.ShouldBe([arrowLeft.CodePoint, arrowLeft.RtlCodePoint!.Value, Icon(editor, "Save", "Filled").Info.CodePoint], ignoreOrder: true);
            subset.Destination.PathDisplay.ShouldEndWith(Path.Combine("Test.Icons_Export", "Test.Icons.otf"));

            exporter.ProjectName.ShouldBe("Test.Icons");
            exporter.DefaultVariant.ShouldBe("Regular");
            exporter.Icons.Select(i => (i.ExportName, i.Icon.Variant)).ShouldBe([("Back", "Regular"), ("Save", "Filled")], ignoreOrder: true);
        });
    }

    private static async Task<(TestNavigator Nav, TestServices Services, string Path)> OpenEditorAsync(params (string GroupId, string ExportName, string[] Variants)[] exports)
        => await OpenEditorAsync(new TestServices(), exports);

    private static async Task<(TestNavigator Nav, TestServices Services, string Path)> OpenEditorAsync(TestServices services, params (string GroupId, string ExportName, string[] Variants)[] exports)
    {
        string path = Path.Combine(TestFiles.NewTempDirectory(), "Test.Icons.ipproj");
        TestFiles.WriteProject(path, exports: exports);

        var nav = BuildNav(services);
        (await nav.NavigateAsync(Routes.EditorRoot.ToConcrete(path))).ShouldBe(NavigationResult.Success);

        return (nav, services, path);
    }

    private static TestNavigator BuildNav(TestServices services) => new(b =>
    {
        b.MapViewModel<StartRootModel>();
        b.MapViewModel<EditorRootModel>();
        b.AddAllRoutes();
        b.Services = services;
    });

    /// <summary>
    /// Waits out the file watcher and filter debounce timers and lets the posted work run.
    /// </summary>
    private static async Task SettleAsync(TestNavigator nav)
    {
        await Task.Delay(1200);
        await nav.WaitUntilIdleAsync();
        await Task.Delay(200);
        await nav.WaitUntilIdleAsync();
    }

    private static IconGroupModel Group(EditorRootModel editor, string id) => editor.FilteredIconGroups.First(g => g.Info.Id == id);

    private static IconModel Icon(EditorRootModel editor, string id, string variant) => Group(editor, id).Icons.First(i => i.Info.Variant == variant);

    private sealed class RecordingExporter : Core.Services.IExporter
    {
        public string Name => "Recording";

        public string? ProjectName { get; private set; }

        public string? DefaultVariant { get; private set; }

        public List<Core.Services.ExportIconInfo> Icons { get; } = [];

        public Task SaveAsync(string projectName, Singulink.IO.IAbsoluteDirectoryPath exportDir, IEnumerable<Core.Services.ExportIconInfo> icons, string defaultVariantName)
        {
            ProjectName = projectName;
            DefaultVariant = defaultVariantName;
            Icons.AddRange(icons);
            return Task.CompletedTask;
        }
    }
}
