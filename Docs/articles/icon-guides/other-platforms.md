<div class="article">

# Other Platforms

The dedicated packages are conveniences, not requirements. An icon from a pack is just a code point in a font, so any framework that can draw text with a custom font can use the base **Singulink.UI.Icons** package directly: read <xref:Singulink.UI.Icons.IIconGlyph.Glyph> (or <xref:Singulink.UI.Icons.IIconGlyph.CodePoint>), set the icon font, and use <xref:Singulink.UI.Icons.IconGlyphExtensions.GetGlyph(Singulink.UI.Icons.IIconGlyph,System.Boolean)> to pick the right-to-left glyph when the framework reports a right-to-left layout. Windows Forms is shown below as a worked example; the same pattern applies to console rendering libraries, game engines, PDF generators and anything else with a text API. Web apps get a stylesheet and a JavaScript module with every export, described in [Web Apps](#web-apps).

## Windows Forms

A font icon in Windows Forms is text drawn with the icon font, and the control's ambient `RightToLeft` setting decides which glyph to draw.

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

## Web Apps

Every export also writes a stylesheet and a JavaScript module for the pack, so web apps use the same icons and the same names as the .NET code. Copy the font, `<Class>.css` and (if needed) `<Class>.js` with `<Class>.d.ts` into the app's static assets, keeping the font next to the stylesheet since the `@font-face` rule refers to it by file name.

### CSS Classes

The stylesheet declares the font, a base class named after the pack's class in kebab-case, and one class per icon that renders the glyph in a `::before` pseudo-element. Member names map to class names the same way, so `FontIcons.SaveFilled` in C# is `font-icons-save-filled` in CSS:

```html
<link rel="stylesheet" href="icons/FontIcons.css" />

<button><i class="font-icons font-icons-save-filled" aria-hidden="true"></i> Save</button>
```

Icons with a distinct right-to-left glyph get a second rule that applies it when the element's directionality is right-to-left, so a document (or subtree) with `dir="rtl"` mirrors them automatically:

```css
.font-icons-back::before { content: "\F0048"; }
.font-icons-back:dir(rtl)::before { content: "\100048"; }
```

### JavaScript and TypeScript

The module exports an object with the same members as the C# class. Each member carries the icon's CSS class name (for frameworks that render class names) and its glyphs (for rendering the icon font directly, canvas text, tooltips and so on). The module is plain JavaScript that runs in any browser or bundler, and the declaration file next to it gives TypeScript projects full typing without a build step:

```js
import { FontIcons, getGlyph } from "./icons/FontIcons.js";

button.innerHTML = `<i class="font-icons ${FontIcons.SaveFilled.className}"></i> Save`;
context.fillText(getGlyph(FontIcons.Back, document.dir === "rtl"), x, y);
```

### Blazor

Blazor apps can use the stylesheet classes as above, or render the glyph from the exported C# class directly, picking the right-to-left glyph when the document (or an ancestor) is `dir="rtl"`:

```razor
<span class="font-icons" aria-hidden="true">@FontIcons.Back.GetGlyph(IsRightToLeft)</span>

@code {
    [CascadingParameter] public bool IsRightToLeft { get; set; }
}
```

</div>
