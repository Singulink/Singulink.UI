<div class="article">

# WinUI and Uno Platform

Reference **Singulink.UI.Icons.WinUI** from the UI project. It supports WinUI 3 (WinAppSDK) and every Uno Platform head from a single package.

### Font Setup

Add the exported font to the project's assets and declare a font family resource, for example in `App.xaml`:

```xml
<FontFamily x:Key="IconFont">ms-appx:///Assets/Fonts/MyApp.FontIcons.otf#Seagull Fluent Icons</FontFamily>
```

A style keeps icon markup short:

```xml
<Style x:Key="Icon" TargetType="FontIcon">
  <Setter Property="FontFamily" Value="{StaticResource IconFont}" />
  <Setter Property="FontSize" Value="16" />
</Style>
```

Styles with a `FontIcon` target type apply to <xref:Singulink.UI.Icons.WinUI.AutoDirectionFontIcon> as well, since it derives from `FontIcon`.

### Displaying Icons

Use <xref:Singulink.UI.Icons.WinUI.AutoDirectionFontIcon> wherever a `FontIcon` is accepted. Its `Glyph` property takes an icon from the generated pack and it displays the right-to-left glyph automatically whenever its effective flow direction is right-to-left:

```xml
xmlns:sui="using:Singulink.UI.Icons.WinUI"
xmlns:icons="using:MyApp"

<sui:AutoDirectionFontIcon Glyph="{x:Bind icons:FontIcons.Back}" Style="{StaticResource Icon}" />

<AppBarButton Label="Save">
  <AppBarButton.Icon>
    <sui:AutoDirectionFontIcon Glyph="{x:Bind icons:FontIcons.Save}" />
  </AppBarButton.Icon>
</AppBarButton>
```

`Glyph` is a normal dependency property, so `{Binding}` and runtime assignment work too. Because the element manages the underlying glyph string itself, do not set the string `Glyph` inherited from `FontIcon` (for example through a `FontIcon`-typed reference or a style setter).

A plain `FontIcon` bound directly to an icon also works, and is what you get by binding the icon to the string `Glyph` property; it always displays the left-to-right glyph:

```xml
<FontIcon Glyph="{x:Bind icons:FontIcons.Save}" Style="{StaticResource Icon}" />
```

### Font Icons You Do Not Own

For a `FontIcon` inside a control template or third-party markup that cannot be replaced, the <xref:Singulink.UI.Icons.WinUI.AutoDirection> attached property gives an existing element the same behaviour:

```xml
<FontIcon sui:AutoDirection.Glyph="{x:Bind icons:FontIcons.Back}" Style="{StaticResource Icon}" />
```

### Icon Sources

Controls such as `TabViewItem`, `InfoBar` and `IconSourceElement` take an `IconSource` rather than an element. The framework creates the actual `FontIcon` from the source itself, so the source cannot observe the flow direction of the element that ends up displaying it. <xref:Singulink.UI.Icons.WinUI.DirectionalFontIconSource> therefore takes the direction explicitly; bind it to the hosting element or the root:

```xml
<TabViewItem Header="Home">
  <TabViewItem.IconSource>
    <sui:DirectionalFontIconSource Icon="{x:Bind icons:FontIcons.Home}" FlowDirection="{x:Bind FlowDirection, Mode=OneWay}"
                                   FontFamily="{StaticResource IconFont}" />
  </TabViewItem.IconSource>
</TabViewItem>
```

Some hosts (`TabViewItem` on both WinUI and Uno, for example) copy the glyph once when they create their icon element and do not observe later changes to the source, so a direction change while the element is showing is not reflected in them. This only matters for apps that switch flow direction at runtime; setting the direction before the page is shown works everywhere.

### Code-Behind

```csharp
var icon = new AutoDirectionFontIcon { Glyph = FontIcons.Back };
AutoDirection.SetGlyph(existingFontIcon, FontIcons.Back);
```

### Uno Platform Notes

Uno mirrors layout for `FlowDirection` on Skia-rendered heads. On Android, iOS and native WebAssembly the layout is not mirrored, but the inherited `FlowDirection` value still reaches every element, so glyph selection continues to follow it.

Uno's flyouts and menus currently do not adopt the flow direction of their placement target (the popup content stays left-to-right), so icons inside them show their left-to-right glyphs even when the page is right-to-left. WinUI's flyouts follow the target, and icons inside them switch correctly.

</div>
