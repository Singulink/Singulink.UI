using IconPackBuilder.ViewModels;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.IO;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.Testing;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class StartRootModelTests
{
    [TestMethod]
    public void RecentProjects_ListedMostRecentFirst_WithMissingFilesMarked()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            string existing = Path.Combine(TestFiles.NewTempDirectory(), "Existing.ipproj");
            TestFiles.WriteProject(existing);
            await services.RecentProjects.AddOrUpdateAsync(@"C:\nowhere\Missing.ipproj", "Missing");
            await services.RecentProjects.AddOrUpdateAsync(existing, "Existing");

            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();

            start.HasRecentProjects.ShouldBeTrue();
            start.RecentProjects.Select(p => p.Name).ShouldBe(["Existing", "Missing"]);
            start.RecentProjects[0].IsMissing.ShouldBeFalse();
            start.RecentProjects[0].Folder.ShouldBe(Path.GetDirectoryName(existing));
            start.RecentProjects[1].IsMissing.ShouldBeTrue();
            start.RecentProjects[1].Opacity.ShouldBeLessThan(1);
        });
    }

    [TestMethod]
    public void OpenRecent_ExistingProject_NavigatesToEditor()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            string path = Path.Combine(TestFiles.NewTempDirectory(), "Existing.ipproj");
            TestFiles.WriteProject(path);
            await services.RecentProjects.AddOrUpdateAsync(path, "Existing");

            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();

            await start.OpenRecentProjectCommand.ExecuteAsync(start.RecentProjects[0]);

            nav.ActiveViewModel<EditorRootModel>().ProjectFile.PathDisplay.ShouldBe(path);
        });
    }

    [TestMethod]
    public void OpenRecent_MissingProject_OffersToRemove()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            await services.RecentProjects.AddOrUpdateAsync(@"C:\nowhere\Missing.ipproj", "Missing");
            await services.RecentProjects.AddOrUpdateAsync(@"C:\nowhere\Other.ipproj", "Other");

            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();
            var prompts = new List<string>();
            int answer = 1;
            nav.OnMessageDialog(m =>
            {
                prompts.Add(m.Message);
                return answer;
            });

            // Keep: the entry stays, no navigation happens.
            await start.OpenRecentProjectCommand.ExecuteAsync(start.RecentProjects.Single(p => p.Name == "Missing"));
            prompts.ShouldHaveSingleItem().ShouldContain("Missing.ipproj");
            start.RecentProjects.Count.ShouldBe(2);
            nav.ActiveViewModel<StartRootModel>().ShouldBeSameAs(start);

            // Remove: the entry is gone from the list and the store.
            answer = 0;
            await start.OpenRecentProjectCommand.ExecuteAsync(start.RecentProjects.Single(p => p.Name == "Missing"));
            start.RecentProjects.Select(p => p.Name).ShouldBe(["Other"]);
            services.RecentProjects.Projects.Select(p => p.Name).ShouldBe(["Other"]);
        });
    }

    [TestMethod]
    public void RemoveAndClear_UpdateListAndStore()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            await services.RecentProjects.AddOrUpdateAsync(@"C:\a\One.ipproj", "One");
            await services.RecentProjects.AddOrUpdateAsync(@"C:\a\Two.ipproj", "Two");

            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();

            await start.RemoveRecentProjectCommand.ExecuteAsync(start.RecentProjects.Single(p => p.Name == "Two"));
            start.RecentProjects.Select(p => p.Name).ShouldBe(["One"]);

            int answer = 1;
            nav.OnMessageDialog(m => answer);

            // Cancel keeps the list.
            await start.ClearRecentProjectsCommand.ExecuteAsync(null);
            start.HasRecentProjects.ShouldBeTrue();

            answer = 0;
            await start.ClearRecentProjectsCommand.ExecuteAsync(null);
            start.HasRecentProjects.ShouldBeFalse();
            start.RecentProjects.ShouldBeEmpty();
            services.RecentProjects.Projects.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void CreateProject_ValidatesName_WritesFile_AndOpensEditor()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            string path = Path.Combine(TestFiles.NewTempDirectory(), "MyApp.Icons.ipproj");
            services.FileDialogs.NextSaveResult = FilePath.ParseAbsolute(path, PathOptions.None);

            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();

            start.NewProjectName = "NoNamespace";
            start.CreateProjectCommand.CanExecute(null).ShouldBeFalse();
            start.NewProjectName = "My App.Icons";
            start.CreateProjectCommand.CanExecute(null).ShouldBeFalse();
            start.NewProjectName = "MyApp.Icons";
            start.CreateProjectCommand.CanExecute(null).ShouldBeTrue();

            await start.CreateProjectCommand.ExecuteAsync(null);

            var project = TestFiles.ReadProject(path);
            project.Name.ShouldBe("MyApp.Icons");
            project.IconsSourceId.ShouldBe(FakeIconsSource.SourceId);
            project.IconExports.ShouldBeEmpty();

            nav.ActiveViewModel<EditorRootModel>().ProjectName.ShouldBe("MyApp.Icons");
            services.RecentProjects.Projects.Single().Name.ShouldBe("MyApp.Icons");
        });
    }

    [TestMethod]
    public void OpenProject_CancelledDialog_StaysOnStart()
    {
        NavigationTestContext.Run(async () =>
        {
            var services = new TestServices();
            var nav = BuildNav(services);
            await nav.NavigateAsync(Routes.StartRoot);
            var start = nav.ActiveViewModel<StartRootModel>();

            await start.OpenProjectCommand.ExecuteAsync(null);

            nav.ActiveViewModel<StartRootModel>().ShouldBeSameAs(start);
        });
    }

    private static TestNavigator BuildNav(TestServices services) => new(b =>
    {
        b.MapViewModel<StartRootModel>();
        b.MapViewModel<EditorRootModel>();
        b.AddAllRoutes();
        b.Services = services;
    });
}
