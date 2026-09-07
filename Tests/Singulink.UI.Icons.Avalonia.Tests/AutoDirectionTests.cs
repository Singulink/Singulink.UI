using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Icons.Avalonia.Tests;

[PrefixTestClass]
public class AutoDirectionTests
{
    private static readonly IconWithRtlGlyph Back = new(0xF0050, 0x100050);
    private static readonly IconWithRtlGlyph Forward = new(0xF0048, 0x100048);
    private static readonly IconGlyph Save = new(0xF03E4);

    [TestMethod]
    public void Element_FollowsInheritedAndLocalFlowDirection()
    {
        HeadlessApp.RunOnUIThread(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back };
            var plain = new AutoDirectionFontIcon { Glyph = Save };
            var window = new Window { Content = new StackPanel { Children = { icon, plain } } };
            window.Show();

            icon.Text.ShouldBe(Back.Glyph);

            window.FlowDirection = FlowDirection.RightToLeft;
            icon.Text.ShouldBe(Back.RtlGlyph);
            plain.Text.ShouldBe(Save.Glyph);

            icon.FlowDirection = FlowDirection.LeftToRight;
            icon.Text.ShouldBe(Back.Glyph);

            icon.ClearValue(Visual.FlowDirectionProperty);
            icon.Text.ShouldBe(Back.RtlGlyph);

            icon.Glyph = Forward;
            icon.Text.ShouldBe(Forward.RtlGlyph);

            icon.Glyph = null;
            icon.Text.ShouldBe(string.Empty);

            window.Close();
        });
    }

    [TestMethod]
    public void Element_ReparentedUnderDifferentDirection_Updates()
    {
        HeadlessApp.RunOnUIThread(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back };
            var ltrHost = new Border { Child = icon };
            var rtlHost = new Border { FlowDirection = FlowDirection.RightToLeft };
            var window = new Window { Content = new StackPanel { Children = { ltrHost, rtlHost } } };
            window.Show();

            icon.Text.ShouldBe(Back.Glyph);

            ltrHost.Child = null;
            rtlHost.Child = icon;
            icon.Text.ShouldBe(Back.RtlGlyph);

            window.Close();
        });
    }

    [TestMethod]
    public void AttachedProperty_FollowsFlowDirection()
    {
        HeadlessApp.RunOnUIThread(() =>
        {
            var textBlock = new TextBlock();
            AutoDirection.SetGlyph(textBlock, Back);
            var window = new Window { Content = textBlock };
            window.Show();

            AutoDirection.GetGlyph(textBlock).ShouldBeSameAs(Back);
            textBlock.Text.ShouldBe(Back.Glyph);

            window.FlowDirection = FlowDirection.RightToLeft;
            textBlock.Text.ShouldBe(Back.RtlGlyph);

            AutoDirection.SetGlyph(textBlock, Forward);
            textBlock.Text.ShouldBe(Forward.RtlGlyph);

            AutoDirection.SetGlyph(textBlock, null);
            textBlock.Text.ShouldBe(string.Empty);

            window.Close();
        });
    }

    [TestMethod]
    public void LayoutAndRender_SucceedWithoutIconFont()
    {
        HeadlessApp.RunOnUIThread(() =>
        {
            var icon = new AutoDirectionFontIcon { Glyph = Back, FontSize = 24 };
            var window = new Window { Content = icon, Width = 100, Height = 100 };
            window.Show();
            window.UpdateLayout();

            icon.Bounds.Height.ShouldBeGreaterThan(0);
            window.Close();
        });
    }
}
