using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.IO;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class CSharpExporterTests
{
    [TestMethod]
    public async Task Save_GeneratesStronglyTypedMembers()
    {
        var dir = DirectoryPath.ParseAbsolute(TestFiles.NewTempDirectory(), PathOptions.None);
        var save = new IconGroupInfo("Save", "Save", [new IconInfo("Regular", 0xF03E4, null), new IconInfo("Filled", 0xF03E5, null)]);
        var back = new IconGroupInfo("ArrowLeft", "Arrow Left", [new IconInfo("Regular", 0xF0048, 0x100048)]);

        await CSharpExporter.Instance.SaveAsync("MyApp.FontIcons", dir, [
            new ExportIconInfo("Save", save.Icons[0]),
            new ExportIconInfo("Save", save.Icons[1]),
            new ExportIconInfo("Back", back.Icons[0]),
        ], defaultVariantName: "Regular");

        string code = File.ReadAllText(Path.Combine(dir.PathExport, "FontIcons.cs"));

        code.ShouldContain("namespace MyApp;");
        code.ShouldContain("public static class FontIcons");
        code.ShouldContain("public static IconGlyph Save { get; } = new(0xF03E4);");
        code.ShouldContain("public static IconGlyph SaveFilled { get; } = new(0xF03E5);");
        code.ShouldContain("public static IconWithRtlGlyph Back { get; } = new(0xF0048, 0x100048);");
        code.ShouldContain("using Singulink.UI.Icons;");
    }

    [TestMethod]
    public async Task Save_ProjectNameWithoutNamespace_UsesItForBoth()
    {
        var dir = DirectoryPath.ParseAbsolute(TestFiles.NewTempDirectory(), PathOptions.None);
        var group = new IconGroupInfo("Add", "Add", [new IconInfo("Regular", 0xF0008, null)]);

        await CSharpExporter.Instance.SaveAsync("Icons", dir, [new ExportIconInfo("Add", group.Icons[0])], "Regular");

        string code = File.ReadAllText(Path.Combine(dir.PathExport, "Icons.cs"));
        code.ShouldContain("namespace Icons;");
        code.ShouldContain("public static class Icons");
    }

    [TestMethod]
    public async Task Save_DuplicateMemberNames_Throw()
    {
        var dir = DirectoryPath.ParseAbsolute(TestFiles.NewTempDirectory(), PathOptions.None);
        var a = new IconGroupInfo("A", "A", [new IconInfo("Regular", 0xF0001, null)]);
        var b = new IconGroupInfo("B", "B", [new IconInfo("Regular", 0xF0002, null)]);

        // Two groups exported under the same name collide.
        await Should.ThrowAsync<InvalidOperationException>(() => CSharpExporter.Instance.SaveAsync(
            "MyApp.Icons", dir, [new ExportIconInfo("Same", a.Icons[0]), new ExportIconInfo("Same", b.Icons[0])], "Regular"));
    }

    [TestMethod]
    public void IconGroupInfo_RejectsEmptyGroups_AndNormalizesMetadata()
    {
        Should.Throw<ArgumentException>(() => new IconGroupInfo("Empty", "Empty", []));

        var group = new IconGroupInfo("Add", "Add", [new IconInfo("Regular", 0xF0008, null)], description: "   ", keywords: null);
        group.Description.ShouldBeNull();
        group.Keywords.ShouldBeEmpty();
        group.HasUniqueRtlGlyphs.ShouldBeFalse();

        // An RTL code point equal to the regular one is treated as "no RTL glyph".
        new IconInfo("Regular", 0xF0008, 0xF0008).RtlCodePoint.ShouldBeNull();
    }
}
