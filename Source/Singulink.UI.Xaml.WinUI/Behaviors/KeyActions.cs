using Microsoft.UI.Xaml.Input;
using Windows.System;

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties for handling key actions on controls.
/// Accessors take a <see cref="DependencyObject"/> rather than the supported element type because the WinUI x:Bind code generator passes attached
/// property targets as <see cref="DependencyObject"/>, so narrower parameter types fail to compile when the property is set with x:Bind. Unsupported
/// targets throw when the property is set.
/// </summary>
public static class KeyActions
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding an <see cref="EnterKeyAction"/> to a <see cref="TextBox"/> or <see cref="PasswordBox"/>.
    /// Deliberately restricted to those types (other control types throw): the behavior rewrites Enter key handling, which is only meaningful on text inputs
    /// and would hijack Enter activation semantics elsewhere. Additional text input types can be supported as needed.
    /// </summary>
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached(
        "Enter", typeof(EnterKeyAction), typeof(KeyActions), new PropertyMetadata(EnterKeyAction.None, OnEnterChanged));

    /// <summary>
    /// Gets the enter key action for the specified <see cref="TextBox"/> or <see cref="PasswordBox"/> control.
    /// </summary>
    public static EnterKeyAction GetEnter(DependencyObject control) => (EnterKeyAction)control.GetValue(EnterProperty);

    /// <summary>
    /// Sets the enter key action for the specified <see cref="TextBox"/> or <see cref="PasswordBox"/> control.
    /// </summary>
    public static void SetEnter(DependencyObject control, EnterKeyAction action) => control.SetValue(EnterProperty, action);

    private static void OnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var action = (EnterKeyAction)e.NewValue;

        if (d is TextBox tb)
        {
            tb.KeyUp -= OnKeyUp;

            if (action != EnterKeyAction.None)
            {
                tb.KeyUp += OnKeyUp;

#if HAS_UNO
                TextBoxExtensions.SetInputReturnType(tb, action == EnterKeyAction.Done ? InputReturnType.Done : InputReturnType.Next);
#endif
            }
        }
        else if (d is PasswordBox pb)
        {
            pb.KeyUp -= OnKeyUp;

            if (action != EnterKeyAction.None)
            {
                pb.KeyUp += OnKeyUp;

#if HAS_UNO
                TextBoxExtensions.SetInputReturnType(pb, action == EnterKeyAction.Done ? InputReturnType.Done : InputReturnType.Next);
#endif
            }
        }
        else
        {
            throw new InvalidOperationException($"The control type '{d.GetType()}' is not supported by the Enter attached property.");
        }

        // A "Next" return key cannot dismiss the soft keyboard on iOS, so SoftKeyboard implies dismissability for it unless SoftKeyboard.Dismissable is
        // explicitly set. The effective state is computed from current values when focus changes, so this only needs to keep its focus hooks current.
        SoftKeyboard.UpdateHooks((Control)d);
    }

    private static void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var control = (Control)sender;

        if (e.Key == VirtualKey.Enter)
        {
            var root = control.XamlRoot?.Content;

            if (GetEnter(control) == EnterKeyAction.Done || root is null || !FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new() { SearchRoot = root }))
                SoftKeyboard.Dismiss(control);

            e.Handled = true;
        }
    }
}
