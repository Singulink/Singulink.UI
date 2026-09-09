<div class="article">

# WPF

Reference **Singulink.UI.Icons.Wpf** from the UI project. WPF has no font icon element, so icons are displayed as text: <xref:Singulink.UI.Icons.Wpf.AutoDirectionFontIcon> is a `TextBlock` that renders an icon from the pack and manages its own `Text`.

### Font Setup

Add the exported font to the project as a `Resource` and declare a font family resource, for example in `App.xaml`. The `#` syntax names the family inside the folder:

```xml
<FontFamily x:Key="IconFont">pack://application:,,,/Assets/Fonts/#Seagull Fluent Icons</FontFamily>

<Style x:Key="Icon" TargetType="{x:Type sui:AutoDirectionFontIcon}">
  <Setter Property="FontFamily" Value="{StaticResource IconFont}" />
  <Setter Property="FontSize" Value="16" />
  <Setter Property="VerticalAlignment" Value="Center" />
</Style>
```

### Displaying Icons

```xml
xmlns:sui="clr-namespace:Singulink.UI.Icons.Wpf;assembly=Singulink.UI.Icons.Wpf"
xmlns:icons="clr-namespace:MyApp"

<sui:AutoDirectionFontIcon Glyph="{x:Static icons:FontIcons.Back}" Style="{StaticResource Icon}" />

<Button>
  <StackPanel Orientation="Horizontal">
    <sui:AutoDirectionFontIcon Glyph="{x:Static icons:FontIcons.Save}" Style="{StaticResource Icon}" Margin="0,0,6,0" />
    <TextBlock Text="Save" />
  </StackPanel>
</Button>
```

The element follows its effective `FlowDirection`, so setting `FlowDirection="RightToLeft"` on a window or any ancestor switches directional icons to their right-to-left glyphs. `Glyph` is a dependency property and can be bound like any other.

### Left-to-Right Only Apps

An app that never runs right-to-left does not need the element at all: bind the generated member to a `TextBlock` with the icon font applied. Bindings convert it to its glyph string, and it always shows the left-to-right glyph.

```xml
<TextBlock Text="{Binding Source={x:Static icons:FontIcons.Back}, Mode=OneTime}" FontFamily="{StaticResource IconFont}" FontSize="16" />
```

### Text Blocks You Do Not Own

For a `TextBlock` inside a template that cannot be replaced, the <xref:Singulink.UI.Icons.Wpf.AutoDirection> attached property provides the same behaviour:

```xml
<TextBlock sui:AutoDirection.Glyph="{x:Static icons:FontIcons.Back}" FontFamily="{StaticResource IconFont}" />
```

### Code-Behind

```csharp
var icon = new AutoDirectionFontIcon { Glyph = FontIcons.Back, FontFamily = (FontFamily)FindResource("IconFont") };
AutoDirection.SetGlyph(existingTextBlock, FontIcons.Back);
```

</div>
