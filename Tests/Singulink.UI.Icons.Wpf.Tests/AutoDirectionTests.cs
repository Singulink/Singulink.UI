using System.Windows;
using System.Windows.Controls;
using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Icons.Wpf.Tests;

[PrefixTestClass]
public class AutoDirectionTests
{
    private static readonly IconWithRtlGlyph Back = new(0xF0050, 0x100050);
    private static readonly IconWithRtlGlyph Forward = new(0xF0048, 0x100048);
    private static readonly IconGlyph Save = new(0xF03E4);

    [TestMethod]
    public void Element_FollowsInheritedAndLocalFlowDirection()
    {
        StaThread.Run(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back };
            var plain = new AutoDirectionFontIcon { Glyph = Save };
            var root = new StackPanel { Children = { icon, plain } };

            icon.Text.ShouldBe(Back.Glyph);

            root.FlowDirection = FlowDirection.RightToLeft;
            icon.Text.ShouldBe(Back.RtlGlyph);
            plain.Text.ShouldBe(Save.Glyph);

            icon.FlowDirection = FlowDirection.LeftToRight;
            icon.Text.ShouldBe(Back.Glyph);

            icon.ClearValue(FrameworkElement.FlowDirectionProperty);
            icon.Text.ShouldBe(Back.RtlGlyph);

            icon.Glyph = Forward;
            icon.Text.ShouldBe(Forward.RtlGlyph);

            icon.Glyph = null;
            icon.Text.ShouldBe(string.Empty);
        });
    }

    [TestMethod]
    public void Element_ReparentedUnderDifferentDirection_Updates()
    {
        StaThread.Run(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back };
            var ltrHost = new Border { Child = icon };
            var rtlHost = new Border { FlowDirection = FlowDirection.RightToLeft };
            _ = new StackPanel { Children = { ltrHost, rtlHost } };

            icon.Text.ShouldBe(Back.Glyph);

            ltrHost.Child = null;
            rtlHost.Child = icon;
            icon.Text.ShouldBe(Back.RtlGlyph);
        });
    }

    [TestMethod]
    public void AttachedProperty_FollowsFlowDirection()
    {
        StaThread.Run(() =>
        {
            var textBlock = new TextBlock();
            var root = new Border { Child = textBlock };
            AutoDirection.SetGlyph(textBlock, Back);

            AutoDirection.GetGlyph(textBlock).ShouldBeSameAs(Back);
            textBlock.Text.ShouldBe(Back.Glyph);

            root.FlowDirection = FlowDirection.RightToLeft;
            textBlock.Text.ShouldBe(Back.RtlGlyph);

            AutoDirection.SetGlyph(textBlock, Forward);
            textBlock.Text.ShouldBe(Forward.RtlGlyph);

            AutoDirection.SetGlyph(textBlock, null);
            textBlock.Text.ShouldBe(string.Empty);
        });
    }

    [TestMethod]
    public void AttachedProperty_OnNonTextBlock_Throws()
    {
        StaThread.Run(() =>
        {
            var button = new Button();
            Should.Throw<InvalidOperationException>(() => button.SetValue(AutoDirection.GlyphProperty, Back));
        });
    }

    [TestMethod]
    public void Element_MeasuresWithoutIconFont()
    {
        StaThread.Run(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back, FontSize = 24 };
            icon.Measure(new Size(100, 100));

            icon.DesiredSize.Height.ShouldBeGreaterThan(0);
        });
    }
}
