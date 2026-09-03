using Microsoft.UI.Xaml.Media;

#if __IOS__
using System.Diagnostics;
using CoreGraphics;
using Microsoft.UI.Xaml.Input;
using UIKit;
#endif

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties and methods for soft keyboard behavior.
/// </summary>
public static class SoftKeyboard
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> that adds a "Done" button in a toolbar above the iOS soft keyboard which dismisses it, for inputs where the
    /// Enter key cannot dismiss the keyboard (e.g. Enter moves to the next field or enters a newline in a multiline text box). No-op on all other platforms
    /// (Android's back button already dismisses the keyboard and other platforms use hardware keyboards).
    /// </summary>
    public static readonly DependencyProperty DismissableProperty = DependencyProperty.RegisterAttached(
        "Dismissable", typeof(bool), typeof(SoftKeyboard), new PropertyMetadata(false, OnDismissableChanged));

    /// <summary>
    /// Gets a value indicating whether the control shows a "Done" button above the iOS soft keyboard that dismisses it.
    /// </summary>
    public static bool GetDismissable(Control control) => (bool)control.GetValue(DismissableProperty);

    /// <summary>
    /// Sets a value indicating whether the control shows a "Done" button above the iOS soft keyboard that dismisses it.
    /// </summary>
    public static void SetDismissable(Control control, bool value) => control.SetValue(DismissableProperty, value);

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
#if __IOS__
        if (d is not Control control)
            return;

        if ((bool)e.NewValue)
        {
            control.GotFocus += OnControlGotFocus;
            control.LostFocus += OnControlLostFocus;
        }
        else
        {
            control.GotFocus -= OnControlGotFocus;
            control.LostFocus -= OnControlLostFocus;
        }
#endif
    }

#if __IOS__
    private static UIToolbar? _toolbar;
    private static WeakReference<Control>? _focusedControl;

    private static void OnControlGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        _focusedControl = new(control);

        // The native input view becomes first responder as part of focus processing, so defer until it exists.
        control.DispatcherQueue.TryEnqueue(() => SetAccessoryOnFirstResponder(attach: true));
    }

    private static void OnControlLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        control.DispatcherQueue.TryEnqueue(() =>
        {
            // Skip clearing when focus moved to another opted-in control (its GotFocus re-applies anyway, but clearing in between makes the toolbar flicker).
            if (control.XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is Control focused && GetDismissable(focused))
                return;

            SetAccessoryOnFirstResponder(attach: false);
        });
    }

    private static void OnDonePressed()
    {
        if (_focusedControl?.TryGetTarget(out var control) is not true)
        {
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
            return;
        }

        control.DispatcherQueue.TryEnqueue(() => Dismiss(control));
    }

    private static void SetAccessoryOnFirstResponder(bool attach)
    {
        var responder = FindFirstResponder();

        if (responder is null)
            return;

        var toolbar = attach ? (_toolbar ??= CreateToolbar()) : null;

        if (responder is UITextField textField)
        {
            if (textField.InputAccessoryView != toolbar)
            {
                textField.InputAccessoryView = toolbar;
                textField.ReloadInputViews();
            }
        }
        else if (responder is UITextView textView)
        {
            if (textView.InputAccessoryView != toolbar)
            {
                textView.InputAccessoryView = toolbar;
                textView.ReloadInputViews();
            }
        }
        else
        {
            // InputAccessoryView is read-only on other responders - log the type so an incompatible Uno text input implementation is identifiable.
            Debug.WriteLine($"SoftKeyboard: first responder is '{responder.GetType()}' - cannot attach accessory view.");
        }
    }

    private static UIToolbar CreateToolbar()
    {
        var toolbar = new UIToolbar(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, 44));

        var done = new UIBarButtonItem(UIBarButtonSystemItem.Done, (_, _) => OnDonePressed());
        toolbar.Items = [new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace), done];
        toolbar.SizeToFit();

        return toolbar;
    }

    private static UIView? FindFirstResponder()
    {
        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
                continue;

            foreach (var window in windowScene.Windows)
            {
                if (FindFirstResponder(window) is { } responder)
                    return responder;
            }
        }

        return null;

        static UIView? FindFirstResponder(UIView view)
        {
            if (view.IsFirstResponder)
                return view;

            foreach (var subview in view.Subviews)
            {
                if (FindFirstResponder(subview) is { } responder)
                    return responder;
            }

            return null;
        }
    }
#endif
}
