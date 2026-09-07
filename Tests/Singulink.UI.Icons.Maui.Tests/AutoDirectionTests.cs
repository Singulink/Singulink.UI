using Microsoft.Maui;
using Microsoft.Maui.Controls;
using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Icons.Maui.Tests;

/// <summary>
/// Exercises the MAUI helpers through Controls.Core only (no handlers or platform), which is enough because effective flow direction is
/// computed by the element tree.
/// </summary>
[PrefixTestClass]
public class AutoDirectionTests
{
    private static readonly IconWithRtlGlyph Back = new(0xF0050, 0x100050);
    private static readonly IconWithRtlGlyph Forward = new(0xF0048, 0x100048);
    private static readonly IconGlyph Save = new(0xF03E4);

    [TestMethod]
    public void AttachedProperty_FollowsInheritedAndLocalFlowDirection()
    {
        var label = new Label();
        var plain = new Label();
        AutoDirection.SetGlyph(label, Back);
        AutoDirection.SetGlyph(plain, Save);
        var layout = new VerticalStackLayout { label, plain };
        var page = new ContentPage { Content = layout };

        AutoDirection.GetGlyph(label).ShouldBeSameAs(Back);
        label.Text.ShouldBe(Back.Glyph);

        page.FlowDirection = FlowDirection.RightToLeft;
        label.Text.ShouldBe(Back.RtlGlyph);
        plain.Text.ShouldBe(Save.Glyph);

        label.FlowDirection = FlowDirection.LeftToRight;
        label.Text.ShouldBe(Back.Glyph);

        label.FlowDirection = FlowDirection.MatchParent;
        label.Text.ShouldBe(Back.RtlGlyph);

        AutoDirection.SetGlyph(label, Forward);
        label.Text.ShouldBe(Forward.RtlGlyph);

        AutoDirection.SetGlyph(label, null);
        label.Text.ShouldBe(string.Empty);
    }

    [TestMethod]
    public void AttachedProperty_ReparentedUnderDifferentDirection_Updates()
    {
        var label = new Label();
        AutoDirection.SetGlyph(label, Back);
        var ltrHost = new ContentView { Content = label };
        var rtlHost = new ContentView { FlowDirection = FlowDirection.RightToLeft };
        _ = new ContentPage { Content = new VerticalStackLayout { ltrHost, rtlHost } };

        label.Text.ShouldBe(Back.Glyph);

        ltrHost.Content = null;
        rtlHost.Content = label;
        label.Text.ShouldBe(Back.RtlGlyph);
    }

    [TestMethod]
    public void AttachedProperty_OnNonLabel_Throws()
    {
        var button = new Button();
        Should.Throw<InvalidOperationException>(() => button.SetValue(AutoDirection.GlyphProperty, Back));
    }

    [TestMethod]
    public void DirectionalFontImageSource_TracksIconAndFlowDirection()
    {
        var source = new DirectionalFontImageSource { Icon = Back };

        source.Glyph.ShouldBe(Back.Glyph);

        source.FlowDirection = FlowDirection.RightToLeft;
        source.Glyph.ShouldBe(Back.RtlGlyph);

        source.Icon = Forward;
        source.Glyph.ShouldBe(Forward.RtlGlyph);

        source.Icon = Save;
        source.Glyph.ShouldBe(Save.Glyph);

        source.FlowDirection = FlowDirection.LeftToRight;
        source.Icon = null;
        source.Glyph.ShouldBe(string.Empty);
    }
}
