<div class="article">

# Right-to-Left Support

Some icons need to look different in right-to-left layouts: a "back" arrow points the other way, a list icon has its bullets on the other side. Microsoft marks these icons as directional and the Seagull font ships a separate right-to-left glyph for each of them, at a code point offset from the regular glyph. Non-directional icons such as checkmarks, clocks and media controls deliberately have no right-to-left version.

### What the Pack Contains

Icon Pack Builder carries this information into the pack without any extra work on your part:

- Icons with a distinct right-to-left glyph are generated as <xref:Singulink.UI.Icons.IconWithRtlGlyph> with both code points, and both glyphs are included in the subset font.
- All other icons are generated as <xref:Singulink.UI.Icons.IconGlyph>. Their <xref:Singulink.UI.Icons.IIconGlyph.RtlGlyph> and <xref:Singulink.UI.Icons.IIconGlyph.RtlCodePoint> return the regular glyph, so code can always ask for the right-to-left glyph without checking first.
- <xref:Singulink.UI.Icons.IIconGlyph.HasUniqueRtlGlyph> tells you whether an icon actually has a different right-to-left version.

The **With RTL Only** filter in the builder shows which icons have one.

### Picking the Right Glyph

<xref:Singulink.UI.Icons.IconGlyphExtensions.GetGlyph(Singulink.UI.Icons.IIconGlyph,System.Boolean)> returns the glyph string for a flow direction, and is all that is needed in code:

```csharp
string glyph = FontIcons.Back.GetGlyph(isRightToLeft: true);
```

The framework packages do this for you based on the element's effective flow direction, so an app that switches to a right-to-left layout gets the correct glyphs everywhere without touching icon markup:

| Framework | Automatic (follows the element's flow direction) | Explicit direction |
| --- | --- | --- |
| WinUI / Uno | `AutoDirectionFontIcon`, `AutoDirection.Glyph` on `FontIcon` | `DirectionalFontIconSource` for icon source slots |
| WPF | `AutoDirectionFontIcon`, `AutoDirection.Glyph` on `TextBlock` | |
| Avalonia | `AutoDirectionFontIcon`, `AutoDirection.Glyph` on `TextBlock` | |
| .NET MAUI | `AutoDirection.Glyph` on `Label` | `DirectionalFontImageSource` for image slots |
| Windows Forms | | `GetGlyph` with the control's `RightToLeft` |

Explicit direction exists where the framework creates the visual itself from a description object (an icon source or image source) that has no place in the visual tree and therefore no flow direction of its own. Bind its direction to the hosting page or element.

### Binding Glyphs Directly

The generated members convert to their regular glyph string through `ToString()`, so binding one straight to a string property such as `FontIcon.Glyph` or `TextBlock.Text` works and is fine for apps that never run right-to-left. It always yields the left-to-right glyph, which is the only difference from using the framework packages.

</div>
