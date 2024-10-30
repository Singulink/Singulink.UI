using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties for working with flyouts.
/// </summary>
public static class Flyouts
{
    #region CloseAllOnClick

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for specifying whether all open flyouts should be closed when a control inheriting from <see
    /// cref="ButtonBase"/> or <see cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static readonly DependencyProperty CloseAllOnClickProperty = DependencyProperty.RegisterAttached(
        "CloseAllOnClick",
        typeof(bool),
        typeof(Flyouts),
        new PropertyMetadata(false, OnCloseAllOnClickChanged));

    /// <summary>
    /// Gets a value indicating whether all open flyouts should be closed when the specified control inheriting from <see cref="ButtonBase"/> or <see
    /// cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static bool GetCloseAllOnClick(Control element)
    {
        return (bool)element.GetValue(CloseAllOnClickProperty);
    }

    /// <summary>
    /// Sets a value indicating whether all open flyouts should be closed when the specified control inheriting from <see cref="ButtonBase"/> or <see
    /// cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static void SetCloseAllOnClick(Control element, bool value)
    {
        element.SetValue(CloseAllOnClickProperty, value);
    }

    private static void OnCloseAllOnClickChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is ButtonBase button)
        {
            if (e.NewValue.Equals(true))
                button.Click += OnCloseAllClick;
            else
                button.Click -= OnCloseAllClick;
        }
        else if (obj is MenuFlyoutItem menuFlyoutItem)
        {
            if (e.NewValue.Equals(true))
                menuFlyoutItem.Click += OnCloseAllClick;
            else
                menuFlyoutItem.Click -= OnCloseAllClick;
        }
        else
        {
            throw new InvalidOperationException($"The control type '{obj.GetType()}' is not supported by the CloseAllOnClick attached property.");
        }
    }

    private static void OnCloseAllClick(object sender, RoutedEventArgs e)
    {
        var element = (UIElement)sender;
        var flyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot).Where(p => p.IsLightDismissEnabled);

        foreach (var f in flyouts)
            f.IsOpen = false;
    }

    #endregion

    #region HyperlinkFlyout

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for specifying a <see cref="Flyout"/> that should be shown when a <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static readonly DependencyProperty HyperlinkFlyoutProperty = DependencyProperty.RegisterAttached(
        "HyperlinkFlyout",
        typeof(Flyout),
        typeof(Flyouts),
        new PropertyMetadata(false, OnHyperlinkFlyoutChanged));

    /// <summary>
    /// Gets the <see cref="Flyout"/> that should be shown when the specified <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static Flyout GetHyperlinkFlyout(Hyperlink element)
    {
        return (Flyout)element.GetValue(HyperlinkFlyoutProperty);
    }

    /// <summary>
    /// Sets the <see cref="Flyout"/> that should be shown when the specified <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static void SetHyperlinkFlyout(Hyperlink element, Flyout value)
    {
        element.SetValue(HyperlinkFlyoutProperty, value);
    }

    private static void OnHyperlinkFlyoutChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var hyperlink = (Hyperlink)obj;

        hyperlink.Click -= OnHyperlinkFlyoutClick;

        if (e.NewValue != null)
            hyperlink.Click += OnHyperlinkFlyoutClick;
    }

    private static void OnHyperlinkFlyoutClick(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        var textBlock = (TextBlock)VisualTreeHelper.GetParent(sender);
        var flyout = GetHyperlinkFlyout(sender);

        flyout.ShowAt(textBlock);
    }

    #endregion
}
