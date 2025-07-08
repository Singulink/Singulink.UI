using Microsoft.UI.Xaml.Input;
using Windows.System;

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties for handling key actions on controls.
/// </summary>
public static class KeyActions
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding an <see cref="EnterKeyAction"/> to a <see cref="TextBox"/> or <see cref="PasswordBox"/>.
    /// </summary>
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached(
        "Enter", typeof(EnterKeyAction), typeof(KeyActions), new PropertyMetadata(EnterKeyAction.None, OnEnterChanged));

    /// <summary>
    /// Gets the enter key action for the specified <see cref="TextBox"/> or <see cref="PasswordBox"/> control.
    /// </summary>
    public static EnterKeyAction GetEnter(Control control) => (EnterKeyAction)control.GetValue(EnterProperty);

    /// <summary>
    /// Sets the enter key action for the specified <see cref="TextBox"/> or <see cref="PasswordBox"/> control.
    /// </summary>
    public static void SetEnter(Control control, EnterKeyAction action) => control.SetValue(EnterProperty, action);

    private static void OnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var action = (EnterKeyAction)e.NewValue;

        if (d is TextBox tb)
        {
            tb.KeyUp -= OnKeyUp;

            if (action == EnterKeyAction.None)
                return;

            tb.KeyUp += OnKeyUp;

#if HAS_UNO
            TextBoxExtensions.SetInputReturnType(tb, action == EnterKeyAction.Done ? InputReturnType.Done : InputReturnType.Next);
#endif
        }
        else if (d is PasswordBox pb)
        {
            pb.KeyUp -= OnKeyUp;

            if (action != EnterKeyAction.None)
                return;

            pb.KeyUp += OnKeyUp;

#if HAS_UNO
            TextBoxExtensions.SetInputReturnType(pb, action == EnterKeyAction.Done ? InputReturnType.Done : InputReturnType.Next);
#endif
        }
        else
        {
            throw new ArgumentException($"Unsupported control type '{d.GetType()}'.", nameof(d));
        }
    }

    private static void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var control = (Control)sender;

        if (e.Key == VirtualKey.Enter)
        {
            var root = control.XamlRoot?.Content;

            if (GetEnter(control) == EnterKeyAction.Done || root is null || !FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new() { SearchRoot = root }))
            {
                bool isTabStop = control.IsTabStop;
                control.IsTabStop = false;
                control.IsEnabled = false;
                control.IsEnabled = true;
                control.IsTabStop = isTabStop;
            }

            e.Handled = true;
        }
    }
}
