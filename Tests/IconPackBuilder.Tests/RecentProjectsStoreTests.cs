using System.Globalization;
using IconPackBuilder.Core.Services;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class RecentProjectsStoreTests
{
    [TestMethod]
    public async Task AddOrUpdate_MostRecentFirst_DedupesPathsCaseInsensitively()
    {
        var store = new RecentProjectsStore(TestFiles.NewTempPath("recent", ".json"));

        await store.AddOrUpdateAsync(@"C:\a\First.ipproj", "First");
        await store.AddOrUpdateAsync(@"C:\a\Second.ipproj", "Second");
        await store.AddOrUpdateAsync(@"c:\A\FIRST.ipproj", "First Again");

        store.Projects.Select(p => p.Name).ShouldBe(["First Again", "Second"]);
        store.Projects[0].Path.ShouldBe(@"c:\A\FIRST.ipproj");
    }

    [TestMethod]
    public async Task AddOrUpdate_KeepsAtMostTen()
    {
        var store = new RecentProjectsStore(TestFiles.NewTempPath("recent", ".json"));

        for (int i = 0; i < 12; i++)
            await store.AddOrUpdateAsync($@"C:\p\{i}.ipproj", i.ToString(CultureInfo.InvariantCulture));

        store.Projects.Count.ShouldBe(10);
        store.Projects[0].Name.ShouldBe("11");
        store.Projects[^1].Name.ShouldBe("2");
    }

    [TestMethod]
    public async Task RemoveAndClear()
    {
        var store = new RecentProjectsStore(TestFiles.NewTempPath("recent", ".json"));
        await store.AddOrUpdateAsync(@"C:\a\First.ipproj", "First");
        await store.AddOrUpdateAsync(@"C:\a\Second.ipproj", "Second");

        await store.RemoveAsync(@"C:\A\first.ipproj");
        store.Projects.Select(p => p.Name).ShouldBe(["Second"]);

        await store.ClearAsync();
        store.Projects.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Persists_AcrossInstances()
    {
        string path = TestFiles.NewTempPath("recent", ".json");
        var store = new RecentProjectsStore(path);
        await store.AddOrUpdateAsync(@"C:\a\First.ipproj", "First");

        var reloaded = new RecentProjectsStore(path);
        reloaded.Projects.Single().Name.ShouldBe("First");
        reloaded.Projects.Single().LastOpenedUtc.ShouldBe(store.Projects.Single().LastOpenedUtc, TimeSpan.FromMilliseconds(1));
        File.Exists(path + ".tmp").ShouldBeFalse();
    }

    [TestMethod]
    public async Task UnreadableFile_YieldsEmptyList_AndIsReplacedOnSave()
    {
        string path = TestFiles.NewTempPath("recent", ".json");
        File.WriteAllText(path, "{ not json");

        var store = new RecentProjectsStore(path);
        store.Projects.ShouldBeEmpty();

        await store.AddOrUpdateAsync(@"C:\a\First.ipproj", "First");
        new RecentProjectsStore(path).Projects.Single().Name.ShouldBe("First");
    }
}
