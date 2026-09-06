#pragma warning disable CA1305 // Specify IFormatProvider

using System.Text;
using Microsoft.UI.Xaml.Media;
using Playground.ViewModels.IconsTest;
using Singulink.UI.Icons;
using Singulink.UI.Icons.WinUI;

namespace Playground.Views.IconsTest;

public sealed partial class IconsTestPage : UserControl
{
    public IconsTestViewModel Model => (IconsTestViewModel)DataContext;

    public IconsTestPage()
    {
        InitializeComponent();
    }

    private void OnRtlToggled(object sender, RoutedEventArgs e)
    {
        Container.FlowDirection = RtlToggle.IsOn ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private async void OnRunChecksClick(object sender, RoutedEventArgs e)
    {
        // Exercises every icon element on the page by switching direction and opening the flyout, so the page visibly changes while it runs.
        RunChecksButton.IsEnabled = false;

        try
        {
            Results.Text = await RunChecksAsync();
        }
        catch (Exception ex)
        {
            Results.Text = ex.ToString();
        }
        finally
        {
            RtlToggle.IsOn = false;
            RunChecksButton.IsEnabled = true;
        }
    }

    private async Task<string> RunChecksAsync()
    {
        var sb = new StringBuilder();
        string ltr = TestIcons.ArrowPrevious.Glyph;
        string rtl = TestIcons.ArrowPrevious.RtlGlyph;

        static string Base(FontIcon icon) => (string)icon.GetValue(FontIcon.GlyphProperty);
        static string Show(string? s) => s is null ? "<null>" : s.Length is 0 ? "<empty>" : string.Join(' ', s.EnumerateRunes().Select(r => $"U+{r.Value:X}"));

        void Check(string name, string? actual, string expected)
            => sb.AppendLine($"{(actual == expected ? "PASS" : "FAIL")} {name}: {Show(actual)} (expected {Show(expected)})");

        // Hosts may re-create their icon element when the source changes, so resolve on every check.
        FontIcon? SourceIcon() => FindDescendant<FontIcon>(SourceElement);
        FontIcon? TabIcon() => FindDescendant<FontIcon>(TabItem);

        sb.AppendLine($"IconSourceElement child: {SourceIcon()?.GetType().Name ?? "<none>"}, TabViewItem icon: {TabIcon()?.GetType().Name ?? "<none>"}");

        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft, FlowDirection.LeftToRight })
        {
            Container.FlowDirection = direction;
            await Task.Delay(100);

            string expected = direction is FlowDirection.RightToLeft ? rtl : ltr;
            string prefix = direction is FlowDirection.RightToLeft ? "RTL" : "LTR";

            Check($"{prefix} plain FontIcon (baseline, always LTR)", Base(PlainIcon), ltr);
            Check($"{prefix} AutoDirectionFontIcon x:Bind", Base(AutoIcon), expected);
            Check($"{prefix} AutoDirectionFontIcon Binding", Base(BoundIcon), expected);
            Check($"{prefix} AutoDirectionFontIcon without RTL glyph", Base(NoRtlIcon), TestIcons.Save.Glyph);
            Check($"{prefix} FontIcon attached property", Base(AttachedIcon), expected);
            Check($"{prefix} IconSourceElement", SourceIcon() is { } si ? Base(si) : null, expected);
            Check($"{prefix} TabViewItem icon source", TabIcon() is { } ti ? Base(ti) : null, expected);
        }

        // Runtime set through the shadowed property.
        AutoIcon.Glyph = TestIcons.Save;
        Check("Swap icon at runtime", Base(AutoIcon), TestIcons.Save.Glyph);
        AutoIcon.Glyph = TestIcons.ArrowPrevious;

        // Base-typed access still sees the string.
        FontIcon asBase = AutoIcon;
        sb.AppendLine($"Base-typed Glyph: {asBase.Glyph.GetType().Name} {Show(asBase.Glyph)}; derived Glyph: {AutoIcon.Glyph?.GetType().Name}");

        // Icon source glyph change propagates to created elements.
        var source = (DirectionalFontIconSource)SourceElement.IconSource;
        source.Icon = TestIcons.Save;
        Check("Icon source swap propagates", SourceIcon() is { } si2 ? Base(si2) : null, TestIcons.Save.Glyph);
        source.Icon = TestIcons.ArrowPrevious;

        // Flyout content lives in a popup: open it after switching direction and report what the icon observes.
        foreach (var direction in new[] { FlowDirection.RightToLeft, FlowDirection.LeftToRight })
        {
            Container.FlowDirection = direction;
            FlyoutButton.Flyout.ShowAt(FlyoutButton);
            await Task.Delay(400);

            string prefix = direction is FlowDirection.RightToLeft ? "RTL" : "LTR";
            string expected = direction is FlowDirection.RightToLeft ? rtl : ltr;
            Check($"{prefix} flyout menu icon (opened after switch)", Base(FlyoutMenuIcon), expected);
            FlyoutButton.Flyout.Hide();
            await Task.Delay(300);
        }

        // Menu bar flyout icons are not in the tree until shown; just report what was created.
        sb.AppendLine($"MenuFlyoutItem.Icon: {MenuItem.Icon?.GetType().Name ?? "<none>"} {(MenuItem.Icon is FontIcon mi ? Show(Base(mi)) : string.Empty)}");

        return sb.ToString();
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : class
    {
        int count = VisualTreeHelper.GetChildrenCount(root);

        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T match)
                return match;

            if (FindDescendant<T>(child) is { } found)
                return found;
        }

        return null;
    }
}
