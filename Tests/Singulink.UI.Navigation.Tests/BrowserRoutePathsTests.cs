using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class BrowserRoutePathsTests
{
    [TestMethod]
    [DataRow("/", "/index.html", "/")]
    [DataRow("/app/", "/app/index.html", "/app/")]
    [DataRow("/app", "/app/index.html", "/app/")]
    [DataRow(" /app/ ", "/app/index.html", "/app/")]
    [DataRow("https://cdn.example.com/app", "/index.html", "/app/")]
    public void ResolveBasePath_RootedValue_IsUsed(string configured, string documentPath, string expected)
    {
        BrowserRoutePaths.ResolveBasePath(configured, documentPath).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(null, "/index.html", "/")]
    [DataRow("", "/", "/")]
    [DataRow("./", "/app/index.html", "/app/")]
    [DataRow("./", "/app/", "/app/")]
    [DataRow("app/", "/app/index.html", "/app/")]
    public void ResolveBasePath_RelativeOrMissingValue_UsesDocumentFolder(string? configured, string documentPath, string expected)
    {
        BrowserRoutePaths.ResolveBasePath(configured, documentPath).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("/", "Login", "/Login")]
    [DataRow("/", "/Login", "/Login")]
    [DataRow("/", "", "/")]
    [DataRow("/app/", "Login?x=1#top", "/app/Login?x=1#top")]
    [DataRow("/app/", "/Login", "/app/Login")]
    [DataRow("/app/", "", "/app/")]
    public void ToBrowserUrl_PrefixesBasePath(string basePath, string route, string expected)
    {
        BrowserRoutePaths.ToBrowserUrl(basePath, route).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("/", "/Login?x=1#top", "/Login?x=1#top")]
    [DataRow("/", "/", "/")]
    [DataRow("/app/", "/app/Login?x=1#top", "/Login?x=1#top")]
    [DataRow("/app/", "/app/", "/")]
    [DataRow("/app/", "/app", "/")]
    [DataRow("/app/", "/app?x=1", "/?x=1")]
    [DataRow("/app/", "/app#top", "/#top")]
    [DataRow("/app/", "/APP/Login", "/Login")]
    public void ToRoute_StripsBasePath(string basePath, string browserPath, string expected)
    {
        BrowserRoutePaths.ToRoute(basePath, browserPath).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("/app/", "/application/Login")]
    [DataRow("/app/", "/other/Login")]
    [DataRow("/app/", "/Login")]
    public void ToRoute_PathOutsideBasePath_IsUnchanged(string basePath, string browserPath)
    {
        BrowserRoutePaths.ToRoute(basePath, browserPath).ShouldBe(browserPath);
    }
}
