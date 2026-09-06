using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace Singulink.UI.Icons.Maui;

/// <summary>
/// Attached properties that make a <see cref="Label"/> display an <see cref="IIconGlyph"/> from an icon font, automatically using its right-to-left
/// glyph when the label's effective flow direction is right-to-left. Set <see cref="Label.FontFamily"/> to the icon pack's font. The
/// <see cref="Label.Text"/> property is managed by the attached property and should not be set.
/// </summary>
public static class AutoDirection
{
    /// <summary>
    /// Identifies the <c>AutoDirection.Glyph</c> attached property.
    /// </summary>
    public static readonly BindableProperty GlyphProperty = BindableProperty.CreateAttached(
        "Glyph", typeof(IIconGlyph), typeof(AutoDirection), null, propertyChanged: OnIconChanged);

    private static readonly BindableProperty IsHookedProperty = BindableProperty.CreateAttached(
        "IsHooked", typeof(bool), typeof(AutoDirection), false);

    /// <summary>
    /// Gets the icon displayed by the label.
    /// </summary>
    public static IIconGlyph? GetGlyph(BindableObject label) => (IIconGlyph?)label.GetValue(GlyphProperty);

    /// <summary>
    /// Sets the icon displayed by the label.
    /// </summary>
    public static void SetGlyph(BindableObject label, IIconGlyph? value) => label.SetValue(GlyphProperty, value);

    private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Label label)
            throw new InvalidOperationException($"{nameof(AutoDirection)}.Icon can only be set on a {nameof(Label)}.");

        if (!(bool)label.GetValue(IsHookedProperty))
        {
            label.SetValue(IsHookedProperty, true);
            label.PropertyChanged += OnLabelPropertyChanged;
        }

        UpdateText(label);
    }

    private static void OnLabelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Raised for effective (inherited) flow direction changes as well as local ones.
        if (e.PropertyName == VisualElement.FlowDirectionProperty.PropertyName)
            UpdateText((Label)sender!);
    }

    private static void UpdateText(Label label)
    {
        bool isRightToLeft = (((IVisualElementController)label).EffectiveFlowDirection & EffectiveFlowDirection.RightToLeft) != 0;
        label.Text = GetGlyph(label)?.GetGlyph(isRightToLeft) ?? string.Empty;
    }
}
