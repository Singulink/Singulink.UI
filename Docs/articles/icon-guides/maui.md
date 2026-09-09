<div class="article">

# .NET MAUI

Reference **Singulink.UI.Icons.Maui** from the app project. MAUI displays font glyphs either as text in a `Label` or as an image through `FontImageSource`, and the package supports both.

### Font Setup

Add the exported font to `Resources/Fonts` and register it with an alias in `MauiProgram`:

```csharp
builder.ConfigureFonts(fonts => {
    fonts.AddFont("MyApp.FontIcons.otf", "IconFont");
});
```

### Labels

The <xref:Singulink.UI.Icons.Maui.AutoDirection> attached property makes a `Label` display an icon and manages its `Text`. It follows the label's effective flow direction, including a direction inherited from the page:

```xml
xmlns:sui="clr-namespace:Singulink.UI.Icons.Maui;assembly=Singulink.UI.Icons.Maui"
xmlns:icons="clr-namespace:MyApp"

<Label sui:AutoDirection.Glyph="{x:Static icons:FontIcons.Back}" FontFamily="IconFont" FontSize="20" />
```

### Left-to-Right Only Apps

An app that never runs right-to-left does not need the attached property: bind the generated member to a `Label` with the icon font applied. Bindings convert it to its glyph string, and it always shows the left-to-right glyph. The same works for `FontImageSource.Glyph` in place of `DirectionalFontImageSource` below.

```xml
<Label Text="{Binding Source={x:Static icons:FontIcons.Back}}" FontFamily="IconFont" FontSize="20" />
```

### Images, Buttons and Toolbar Items

Anything that takes an `ImageSource` (`Image`, `Button.ImageSource`, `ToolbarItem.IconImageSource`, tab icons) can use <xref:Singulink.UI.Icons.Maui.DirectionalFontImageSource>. An image source is not a visual element and has no flow direction of its own, so bind its `FlowDirection` to the page (named `Page` here with `x:Name`) or to the hosting element:

```xml
<Button Text="Save">
  <Button.ImageSource>
    <sui:DirectionalFontImageSource Icon="{x:Static icons:FontIcons.Save}" FontFamily="IconFont" Size="20"
                                    FlowDirection="{Binding FlowDirection, Source={x:Reference Page}}" />
  </Button.ImageSource>
</Button>
```

### Code

```csharp
AutoDirection.SetGlyph(label, FontIcons.Back);

var source = new DirectionalFontImageSource { Icon = FontIcons.Save, FontFamily = "IconFont", FlowDirection = page.FlowDirection };
```

</div>
