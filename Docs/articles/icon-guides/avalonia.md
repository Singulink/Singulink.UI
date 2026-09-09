<div class="article">

# Avalonia

Reference **Singulink.UI.Icons.Avalonia** from the UI project (built against Avalonia 11.3, verified on 11.3 and 12.1). Icons are displayed as text: <xref:Singulink.UI.Icons.Avalonia.AutoDirectionFontIcon> is a `TextBlock` that renders an icon from the pack and manages its own `Text`.

### Font Setup

Add the exported font to the project as an `AvaloniaResource` and declare a font family resource, for example in `App.axaml`:

```xml
<FontFamily x:Key="IconFont">avares://MyApp/Assets/Fonts/MyApp.FontIcons.otf#Seagull Fluent Icons</FontFamily>

<Style Selector="sui|AutoDirectionFontIcon">
  <Setter Property="FontFamily" Value="{StaticResource IconFont}" />
  <Setter Property="FontSize" Value="16" />
  <Setter Property="VerticalAlignment" Value="Center" />
</Style>
```

### Displaying Icons

```xml
xmlns:sui="clr-namespace:Singulink.UI.Icons.Avalonia;assembly=Singulink.UI.Icons.Avalonia"
xmlns:icons="clr-namespace:MyApp"

<sui:AutoDirectionFontIcon Glyph="{x:Static icons:FontIcons.Back}" />

<Button>
  <StackPanel Orientation="Horizontal" Spacing="6">
    <sui:AutoDirectionFontIcon Glyph="{x:Static icons:FontIcons.Save}" />
    <TextBlock Text="Save" />
  </StackPanel>
</Button>
```

The control follows its effective `FlowDirection`, so setting `FlowDirection="RightToLeft"` on a window or any ancestor switches directional icons to their right-to-left glyphs. `Glyph` is a styled property and can be bound or set from styles like any other.

### Left-to-Right Only Apps

An app that never runs right-to-left does not need the control at all: bind the generated member to a `TextBlock` with the icon font applied. Bindings convert it to its glyph string, and it always shows the left-to-right glyph.

```xml
<TextBlock Text="{Binding Source={x:Static icons:FontIcons.Back}}" FontFamily="{StaticResource IconFont}" FontSize="16" />
```

### Text Blocks You Do Not Own

For a `TextBlock` inside a template that cannot be replaced, the <xref:Singulink.UI.Icons.Avalonia.AutoDirection> attached property provides the same behaviour:

```xml
<TextBlock sui:AutoDirection.Glyph="{x:Static icons:FontIcons.Back}" FontFamily="{StaticResource IconFont}" />
```

### Code-Behind

```csharp
var icon = new AutoDirectionFontIcon { Glyph = FontIcons.Back };
AutoDirection.SetGlyph(existingTextBlock, FontIcons.Back);
```

</div>
