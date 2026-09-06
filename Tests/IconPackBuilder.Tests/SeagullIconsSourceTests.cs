using IconPackBuilder.Core.IconSources;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

/// <summary>
/// Validates the generated Seagull assets that ship with the builder app, through the source that consumes them.
/// </summary>
[PrefixTestClass]
public class SeagullIconsSourceTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, Core.IconGroupInfo>> Groups = new(() =>
        SeagullIconsSource.Instance.LoadIconGroups().ToDictionary(g => g.Id));

    [TestMethod]
    public void Metadata_DescribesTheGeneratedAssets()
    {
        var source = SeagullIconsSource.Instance;

        source.Id.ShouldBe("FluentIcons.Seagull");
        source.FontFamilyName.ShouldBe("Seagull Fluent Icons");
        source.Version.ShouldBeGreaterThanOrEqualTo(new Version(2, 1, 339, 1));
        source.Variants.ShouldBe(["Regular", "Filled", "Color", "Light"]);
        source.Source.UpstreamCommit.Length.ShouldBe(40);
        source.Source.FluentIconsCommonVersion.ShouldBe(source.Version.ToString());
    }

    [TestMethod]
    public void LoadIconGroups_CoversTheSymbolEnum()
    {
        Groups.Value.Count.ShouldBeGreaterThan(2800);
        Groups.Value.Values.ShouldAllBe(g => g.Icons.Count > 0);
        Groups.Value.Values.Count(g => g.HasUniqueRtlGlyphs).ShouldBeGreaterThan(100);
    }

    [TestMethod]
    public void CodePoints_FollowTheSeagullLayout()
    {
        // Symbol.Save == 249: 0xF0000 + 4 * 249 = 0xF03E4 for Regular, +1 for Filled. Same values exported by existing projects.
        var save = Groups.Value["Save"];
        save.Icons.Single(i => i.Variant == "Regular").CodePoint.ShouldBe(0xF03E4);
        save.Icons.Single(i => i.Variant == "Filled").CodePoint.ShouldBe(0xF03E5);
        save.HasUniqueRtlGlyphs.ShouldBeFalse();

        // Symbol.ArrowPrevious == 20 has distinct right-to-left glyphs at +0x10000.
        var arrowPrevious = Groups.Value["ArrowPrevious"];
        var regular = arrowPrevious.Icons.Single(i => i.Variant == "Regular");
        regular.CodePoint.ShouldBe(0xF0050);
        regular.RtlCodePoint.ShouldBe(0x100050);
        arrowPrevious.HasUniqueRtlGlyphs.ShouldBeTrue();
    }

    [TestMethod]
    public void Groups_CarryUpstreamMetadata()
    {
        var qrCode = Groups.Value["QrCode"];
        qrCode.Name.ShouldBe("QR Code");
        qrCode.Description.ShouldNotBeNullOrWhiteSpace();
        qrCode.Keywords.ShouldContain("scan");

        // Renamed upstream icons keep their own (friendly) name but inherit the metadata of the icon they were derived from.
        var homeEmpty = Groups.Value["HomeEmpty"];
        homeEmpty.Name.ShouldBe("Home Empty");
        homeEmpty.Keywords.ShouldContain("house");

        // Names never contain the raw enum spelling of underscores.
        Groups.Value.Values.ShouldAllBe(g => !g.Name.Contains('_'));
    }

    [TestMethod]
    public void IconInfos_AreFreshOnEveryLoad()
    {
        // IconInfo instances are bound to their group, so a source must yield new instances per load rather than shared ones.
        var first = SeagullIconsSource.Instance.LoadIconGroups().First();
        var second = SeagullIconsSource.Instance.LoadIconGroups().First();

        first.Icons[0].ShouldNotBeSameAs(second.Icons[0]);
        first.Icons[0].Group.ShouldBeSameAs(first);
    }
}
