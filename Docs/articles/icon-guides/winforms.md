<div class="article">

# Windows Forms

There is no Windows Forms package because the base **Singulink.UI.Icons** package is all that is needed: a font icon in Windows Forms is text drawn with the icon font, and <xref:Singulink.UI.Icons.IconGlyphExtensions.GetGlyph(Singulink.UI.Icons.IIconGlyph,System.Boolean)> picks the glyph for the control's `RightToLeft` setting.

### Font Setup

Load the exported font from a private font collection so it does not need to be installed on the machine. Keep the collection alive for the lifetime of the app:

```csharp
using System.Drawing.Text;

public static class IconFont
{
    private static readonly PrivateFontCollection Fonts = new();

    static IconFont()
    {
        Fonts.AddFontFile(Path.Combine(AppContext.BaseDirectory, "Assets", "MyApp.FontIcons.otf"));
    }

    public static Font Create(float size) => new(Fonts.Families[0], size, GraphicsUnit.Point);
}
```

### Displaying Icons

Assign the font and the glyph to any control that draws text, such as a `Label` or `Button`. `RightToLeft` is ambient in Windows Forms, so a control reports the value inherited from its parent and raises `RightToLeftChanged` when it changes:

```csharp
public static class IconControlExtensions
{
    public static void SetIcon(this Control control, IIconGlyph icon, float size = 12)
    {
        control.Font = IconFont.Create(size);
        control.Text = icon.GetGlyph(control.RightToLeft == RightToLeft.Yes);

        control.RightToLeftChanged += (s, e) => control.Text = icon.GetGlyph(control.RightToLeft == RightToLeft.Yes);
    }
}

backLabel.SetIcon(FontIcons.Back, 16);
```

### Rendering Notes

The Seagull glyphs live in a supplementary Unicode plane and are encoded as surrogate pairs. Modern GDI text rendering handles them, but if a glyph renders as a box on an older system, set `UseCompatibleTextRendering` to `true` on the control (or globally with `Application.SetCompatibleTextRenderingDefault(true)`) so GDI+ draws the text instead.

</div>
