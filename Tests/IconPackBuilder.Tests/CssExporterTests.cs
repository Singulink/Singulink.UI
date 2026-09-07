using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class CssExporterTests
{
    [TestMethod]
    public async Task Save_GeneratesFontFaceBaseClassAndIconClasses()
    {
        var context = ExporterTestContext.Create("MyApp.FontIcons");
        var save = new IconGroupInfo("Save", "Save", [new IconInfo("Regular", 0xF03E4, null), new IconInfo("Filled", 0xF03E5, null)]);
        var back = new IconGroupInfo("ArrowLeft", "Arrow Left", [new IconInfo("Regular", 0xF0048, 0x100048)]);

        await CssExporter.Instance.SaveAsync(context with {
            Icons = [
                new ExportIconInfo("Save", save.Icons[0]),
                new ExportIconInfo("Save", save.Icons[1]),
                new ExportIconInfo("Back", back.Icons[0]),
            ],
        });

        string css = File.ReadAllText(Path.Combine(context.ExportDir.PathExport, "FontIcons.css"));

        css.ShouldContain("font-family: \"MyApp.FontIcons\";");
        css.ShouldContain("src: url(\"MyApp.FontIcons.otf\") format(\"opentype\");");
        css.ShouldContain(".font-icons {");
        css.ShouldContain(".font-icons-save::before { content: \"\\F03E4\"; }");
        css.ShouldContain(".font-icons-save-filled::before { content: \"\\F03E5\"; }");
        css.ShouldContain(".font-icons-back::before { content: \"\\F0048\"; }");
        css.ShouldContain(".font-icons-back:dir(rtl)::before { content: \"\\100048\"; }");

        // Icons without a mirrored version get no right-to-left rule.
        css.ShouldNotContain(".font-icons-save:dir(rtl)");
    }

    [TestMethod]
    public async Task Save_UnknownFontExtension_Throws()
    {
        var context = ExporterTestContext.Create("MyApp.Icons") with { FontFileName = "MyApp.Icons.bin" };
        await Should.ThrowAsync<InvalidOperationException>(() => CssExporter.Instance.SaveAsync(context));
    }

    [TestMethod]
    public void ClassNames_FollowMemberNames()
    {
        var context = ExporterTestContext.Create("MyApp.Icons");
        CssExporter.GetClassPrefix(context).ShouldBe("icons");
        CssExporter.GetIconClassName(context, "ArrowLeftFilled").ShouldBe("icons-arrow-left-filled");
    }
}
