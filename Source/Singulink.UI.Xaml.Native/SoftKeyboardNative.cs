#if __IOS__
using System.Diagnostics;
using CoreGraphics;
using UIKit;
#endif

namespace Singulink.UI.Xaml;

/// <summary>
/// Provides platform-native soft keyboard functionality. On iOS this manages a "Done" button in a toolbar above the soft keyboard; on all other platforms the
/// members are inert (Android's back button already dismisses the keyboard and other platforms use hardware keyboards).
/// </summary>
public static class SoftKeyboardNative
{
    /// <summary>
    /// Gets a value indicating whether the current platform supports showing a dismiss accessory above the soft keyboard.
    /// </summary>
#if __IOS__
    public static bool IsDismissAccessorySupported => true;
#else
    public static bool IsDismissAccessorySupported => false;
#endif

    /// <summary>
    /// Shows a "Done" dismiss accessory above the soft keyboard for the currently active text input, invoking the specified callback when it is pressed. Must
    /// be called on the UI thread after the text input has become active. No-op on unsupported platforms.
    /// </summary>
    public static void ShowDismissAccessory(Action dismissRequested)
    {
#if __IOS__
        _dismissRequested = dismissRequested;
        SetAccessoryOnFirstResponder(attach: true);
#endif
    }

    /// <summary>
    /// Hides the dismiss accessory shown by <see cref="ShowDismissAccessory(Action)"/>. Must be called on the UI thread. No-op on unsupported platforms.
    /// </summary>
    public static void HideDismissAccessory()
    {
#if __IOS__
        _dismissRequested = null;
        SetAccessoryOnFirstResponder(attach: false);
#endif
    }

#if __IOS__
    private static UIToolbar? _toolbar;
    private static Action? _dismissRequested;

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
            // InputAccessoryView is read-only on other responders - log the type so an incompatible text input implementation is identifiable.
            Debug.WriteLine($"SoftKeyboardNative: first responder is '{responder.GetType()}' - cannot attach accessory view.");
        }
    }

    private static UIToolbar CreateToolbar()
    {
        var toolbar = new UIToolbar(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, 44));

        var done = new UIBarButtonItem(UIBarButtonSystemItem.Done, (_, _) => _dismissRequested?.Invoke());
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
