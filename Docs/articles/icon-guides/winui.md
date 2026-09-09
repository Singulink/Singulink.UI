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

### Left-to-Right Only Apps

An app that never runs right-to-left does not need this package at all: a plain `FontIcon` bound to a generated member shows its left-to-right glyph, since the members convert to that glyph string. Binding the member's `Glyph` property is equivalent.

```xml
<FontIcon Glyph="{x:Bind icons:FontIcons.Save}" Style="{StaticResource Icon}" />
```

### Font Icons You Do Not Own

For a `FontIcon` inside a control template or third-party markup that cannot be replaced, the <xref:Singulink.UI.Icons.WinUI.AutoDirection> attached property gives an existing element the same behaviour:

```xml
<FontIcon sui:AutoDirection.Glyph="{x:Bind icons:FontIcons.Back}" Style="{StaticResource Icon}" />
```

### Icon Sources

Controls such as `TabViewItem`, `InfoBar` and `IconSourceElement` take an `IconSource` rather than an element. The element a host creates from a `FontIconSource` is a plain `FontIcon` that only knows a single glyph string, so the choice between the regular and right-to-left glyph has to be made by the source, and a source is not part of the visual tree, so it has no flow direction of its own to read.

<xref:Singulink.UI.Icons.WinUI.DirectionalFontIconSource> is a `FontIconSource` that takes the icon and a `FlowDirection`. Attach it to the host control with the `AutoDirection.IconSource` attached property instead of setting `IconSource` directly, and the direction is kept in sync with the control, so the icon follows the flow direction like `AutoDirectionFontIcon` does. `TabViewItem`, `InfoBar` and `IconSourceElement` are supported; setting the property on another control throws. Use a separate source instance for each control.

```xml
<TabViewItem Header="Home">
  <sui:AutoDirection.IconSource>
    <sui:DirectionalFontIconSource Icon="{x:Bind icons:FontIcons.Home}" FontFamily="{StaticResource IconFont}" />
  </sui:AutoDirection.IconSource>
</TabViewItem>
```

The source can also be assigned to `IconSource` directly, with the direction bound to the hosting element or the root:

```xml
<TabViewItem Header="Home">
  <TabViewItem.IconSource>
    <sui:DirectionalFontIconSource Icon="{x:Bind icons:FontIcons.Home}" FlowDirection="{x:Bind FlowDirection, Mode=OneWay}"
                                   FontFamily="{StaticResource IconFont}" />
  </TabViewItem.IconSource>
</TabViewItem>
```

With this form, a direction change while the icon is showing reaches `IconSourceElement` (which observes the source) but not `TabViewItem` or `InfoBar`: both copy the glyph once when they build their icon element and only rebuild it when the property is assigned again, which is what `AutoDirection.IconSource` does for them. This only matters for apps that switch flow direction at runtime; a direction set before the page is shown works everywhere.

### Code-Behind

```csharp
var icon = new AutoDirectionFontIcon { Glyph = FontIcons.Back };
AutoDirection.SetGlyph(existingFontIcon, FontIcons.Back);
AutoDirection.SetIconSource(tabViewItem, new DirectionalFontIconSource { Icon = FontIcons.Home, FontFamily = iconFont });
```

### Uno Platform Notes

Uno mirrors layout for `FlowDirection` on Skia-rendered heads. On Android, iOS and native WebAssembly the layout is not mirrored, but the inherited `FlowDirection` value still reaches every element, so glyph selection continues to follow it.

Uno's flyouts and menus currently do not adopt the flow direction of their placement target (the popup content stays left-to-right), so icons inside them show their left-to-right glyphs even when the page is right-to-left. WinUI's flyouts follow the target, and icons inside them switch correctly.

</div>
