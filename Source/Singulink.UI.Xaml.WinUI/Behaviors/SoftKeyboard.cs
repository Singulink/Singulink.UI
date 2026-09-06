using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties and methods for soft keyboard behavior.
/// Accessors take a <see cref="DependencyObject"/> rather than the supported element type because the WinUI x:Bind code generator passes attached
/// property targets as <see cref="DependencyObject"/>, so narrower parameter types fail to compile when the property is set with x:Bind. Unsupported
/// targets throw when the property is set.
/// </summary>
public static class SoftKeyboard
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> that adds a "Done" button in a toolbar above the iOS soft keyboard which dismisses it, for inputs where the
    /// Enter key cannot dismiss the keyboard (e.g. Enter moves to the next field or enters a newline in a multiline text box). No-op on all other platforms
    /// (Android's back button already dismisses the keyboard and other platforms use hardware keyboards). Implied by
    /// <see cref="KeyActions.EnterProperty"/> being set to <see cref="EnterKeyAction.Next"/> unless explicitly set on the control. Can be set on any control,
    /// including containers (focus events bubble, so the toolbar applies whenever focus lands on a text input within it), and is inert on controls that never
    /// open the keyboard.
    /// </summary>
    public static readonly DependencyProperty DismissableProperty = DependencyProperty.RegisterAttached(
        "Dismissable", typeof(bool), typeof(SoftKeyboard), new PropertyMetadata(false, OnDismissableChanged));

    private static WeakReference<Control>? _focusedControl;

    /// <summary>
    /// Gets a value indicating whether the control shows a "Done" button above the iOS soft keyboard that dismisses it.
    /// </summary>
    public static bool GetDismissable(DependencyObject control) => (bool)control.GetValue(DismissableProperty);

    /// <summary>
    /// Sets a value indicating whether the control shows a "Done" button above the iOS soft keyboard that dismisses it.
    /// </summary>
    public static void SetDismissable(DependencyObject control, bool value) => control.SetValue(DismissableProperty, value);

    /// <summary>
    /// Dismisses the soft keyboard by moving focus off the specified control (so that pending focus-loss updates like <c>LostFocus</c>-triggered bindings are
    /// committed) and hiding the keyboard. Focus is parked on the nearest focusable ancestor control that has no interaction side-effects (i.e. not a selector
    /// item or a button).
    /// </summary>
    public static void Dismiss(Control control)
    {
        bool parked = false;

        for (var parent = VisualTreeHelper.GetParent(control); parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is Control candidate && candidate is not SelectorItem && candidate is not ButtonBase &&
                candidate.Focus(FocusState.Programmatic))
            {
                parked = true;
                break;
            }
        }

        if (!parked)
        {
            // Fall back to dropping focus by toggling the control disabled. Less reliable (the framework may move focus to the next focusable control on some
            // platforms), but parking rarely fails since pages and user controls are valid parking targets.

            bool isTabStop = control.IsTabStop;
            control.IsTabStop = false;
            control.IsEnabled = false;
            control.IsEnabled = true;
            control.IsTabStop = isTabStop;
        }

#if HAS_UNO
        // Ensure the keyboard actually hides in case the focus change did not dismiss it (the same mechanism Uno Toolkit's InputExtensions.AutoDismiss uses).
        Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
#endif
    }

    private static void OnDismissableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control)
            throw new InvalidOperationException($"The element type '{d.GetType()}' is not supported by the Dismissable attached property.");

        UpdateHooks(control);
    }

    /// <summary>
    /// Re-evaluates whether the control needs focus hooks for the keyboard dismiss toolbar, based on the effective dismissable state computed from
    /// <see cref="DismissableProperty"/> (when explicitly set) or <see cref="KeyActions.EnterProperty"/> (a "Next" return key cannot dismiss the keyboard, so
    /// dismissability is implied for it). Called when either property changes; no-op on platforms without dismiss accessory support. The toolbar itself is
    /// managed through <see cref="SoftKeyboardNative"/>, which stays platform-built on Skia-rendered heads where this assembly is swapped for its Skia build.
    /// </summary>
    internal static void UpdateHooks(Control control)
    {
        if (!SoftKeyboardNative.IsDismissAccessorySupported)
            return;

        control.GotFocus -= OnControlGotFocus;
        control.LostFocus -= OnControlLostFocus;

        if (GetEffectiveDismissable(control))
        {
            control.GotFocus += OnControlGotFocus;
            control.LostFocus += OnControlLostFocus;
        }
    }

    private static bool GetEffectiveDismissable(Control control) =>
        control.ReadLocalValue(DismissableProperty) != DependencyProperty.UnsetValue
            ? GetDismissable(control)
            : KeyActions.GetEnter(control) == EnterKeyAction.Next;

    private static void OnControlGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        _focusedControl = new(control);

        // The native input view becomes active as part of focus processing, so defer until it exists.
        control.DispatcherQueue.TryEnqueue(() => SoftKeyboardNative.ShowDismissAccessory(OnAccessoryDismissRequested));
    }

    private static void OnControlLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        control.DispatcherQueue.TryEnqueue(() =>
        {
            // Skip hiding when focus moved to another opted-in control (its GotFocus re-applies anyway, but hiding in between makes the toolbar flicker).
            if (control.XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is Control focused && GetEffectiveDismissable(focused))
                return;

            SoftKeyboardNative.HideDismissAccessory();
        });
    }

    private static void OnAccessoryDismissRequested()
    {
        if (_focusedControl?.TryGetTarget(out var control) is true)
            control.DispatcherQueue.TryEnqueue(() => Dismiss(control));
    }
}
