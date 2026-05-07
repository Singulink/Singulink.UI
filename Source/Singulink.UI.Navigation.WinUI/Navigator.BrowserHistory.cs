#if __WASM__

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides WebAssembly-specific browser history synchronization for the navigator.
/// </content>
partial class Navigator
{
    private static bool s_popStateListenerRegistered;
    private static bool s_beforeUnloadListenerRegistered;

    private SynchronizationContext _synchronizationContext;
    private double _seqCounter;
    private double _currentSeq;
    private int _suppressPopstateCount;
    private bool _isPopstateNavigation;
    private bool _isFirstNavigation = true;

    /// <summary>
    /// Gets the current browser route (path, query string, and fragment) without the scheme and host.
    /// </summary>
    public static string GetBrowserRoute()
    {
        var uri = new Uri(BrowserNavigationHelper.GetCurrentUrl());
        return uri.PathAndQuery + uri.Fragment;
    }

    /// <summary>
    /// Captures the current UI synchronization context (called from the navigator constructor on WebAssembly). The static popstate / beforeunload
    /// dispatchers restore it before invoking instance methods.
    /// </summary>
    [MemberNotNull(nameof(_synchronizationContext))]
    private void CaptureSynchronizationContextForJSCallbacks()
    {
        _synchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Synchronization context not found.");
    }

    /// <summary>
    /// Initializes browser history sync. Replaces the current history entry with a known sequence number and registers the popstate listener (once).
    /// </summary>
    private void InitializeBrowserHistorySync()
    {
        _currentSeq = ++_seqCounter;
        BrowserNavigationHelper.ReplaceState(_currentSeq, string.Empty, BrowserNavigationHelper.GetCurrentUrl());

        if (!s_popStateListenerRegistered)
        {
            BrowserNavigationHelper.AddEventListener("popstate", OnPopStateRaw);
            s_popStateListenerRegistered = true;
        }
    }

    /// <summary>
    /// Registers the <c>beforeunload</c> listener (once globally) that blocks tab/window close while the navigator is busy or any view model vetoes
    /// navigating away. The active owner is resolved via <see cref="s_windowClosedHookOwners"/>; conflict detection happens up-front in
    /// <see cref="HookWindowClosedEvents"/>.
    /// </summary>
    private static void InitializeBrowserUnloadGuard()
    {
        if (!s_beforeUnloadListenerRegistered)
        {
            BrowserNavigationHelper.AddEventListener("beforeunload", OnBeforeUnloadRaw);
            s_beforeUnloadListenerRegistered = true;
        }
    }

    private static void OnBeforeUnloadRaw(JSObject evt)
    {
        // On WebAssembly there is at most one window, so the CWT contains at most one entry: the navigator currently owning HookWindowClosedEvents.
        var instance = s_windowClosedHookOwners.FirstOrDefault().Value;

        if (instance is null)
            return;

        // Restore synchronization context (JS callback loses sync context even though we are on UI thread).
        if (SynchronizationContext.Current is null)
            SynchronizationContext.SetSynchronizationContext(instance._synchronizationContext);

        if (!instance.CanMaybeShutDownNow)
        {
            PreventUnload(evt);
            return;
        }

        var probeTask = instance.TryShutDownProbeAsync();

        if (!probeTask.IsCompleted)
        {
            // Probe went async (a view model is awaiting something). We can't await here, so block this close attempt; the user will see the browser's
            // native "Leave site?" prompt. If they choose to stay, the probe will eventually complete and the next close attempt will likely succeed
            // synchronously without prompting.
            PreventUnload(evt);
            return;
        }

        if (probeTask.IsCompletedSuccessfully && !probeTask.Result)
        {
            // A view model synchronously vetoed the navigation away (e.g. unsaved changes).
            PreventUnload(evt);
        }

        // Otherwise the probe completed synchronously with no veto, so let the unload proceed.
    }

    private static void PreventUnload(JSObject evt)
    {
        // Setting returnValue to a non-empty string is the legacy-but-still-honored cross-browser way to trigger the native "Leave site?" prompt without
        // needing a JS module to call event.preventDefault(). The shown text is browser-controlled and cannot be customized.
#pragma warning disable CA1416 // Validate platform compatibility - this entire file is __WASM__-only
        evt.SetProperty("returnValue", true);
#pragma warning restore CA1416
    }

    /// <inheritdoc />
    protected override object? OnNavigationStarting(NavigationType navigationType, NavigatorRoute targetRoute)
    {
        // If this navigation was triggered by a browser popstate (user clicked the browser back/forward button), the browser has already moved its
        // history pointer. We must not call any history APIs here, otherwise we'd double-navigate the browser.
        if (_isPopstateNavigation)
            return null;

        string url = "/" + targetRoute;

        switch (navigationType)
        {
            case NavigationType.New:
                if (_isFirstNavigation || IsRedirecting)
                {
                    // First navigation should replace the entry the browser landed on (no extra back-stack entry). Redirects should also replace, since the
                    // in-app stack treats them as replacements of the current entry rather than additions.
                    _isFirstNavigation = false;
                    BrowserNavigationHelper.ReplaceState(_currentSeq, string.Empty, url);
                    return WasmNavState.ReplacedNew;
                }

                _currentSeq = ++_seqCounter;
                BrowserNavigationHelper.PushState(_currentSeq, string.Empty, url);
                return WasmNavState.PushedNew;

            case NavigationType.Back:
                _suppressPopstateCount++;
                BrowserNavigationHelper.Back();
                return WasmNavState.WentBack;

            case NavigationType.Forward:
                _suppressPopstateCount++;
                BrowserNavigationHelper.Forward();
                return WasmNavState.WentForward;

            case NavigationType.Refresh:
                BrowserNavigationHelper.ReplaceState(_currentSeq, string.Empty, url);
                return WasmNavState.Replaced;

            default:
                return null;
        }
    }

    /// <inheritdoc />
    protected override void OnNavigationCompleted(NavigationType navigationType, NavigatorRoute targetRoute, NavigationResult result, object? state)
    {
        if (_isPopstateNavigation)
        {
            _isPopstateNavigation = false;

            if (result == NavigationResult.Cancelled)
            {
                // The user pressed browser back/forward, but the in-app navigation was cancelled (e.g. by an "unsaved changes" dialog). The browser already
                // moved its pointer, so undo it to keep browser and in-app state in sync.
                _suppressPopstateCount++;

                if (navigationType == NavigationType.Back)
                    BrowserNavigationHelper.Forward();
                else if (navigationType == NavigationType.Forward)
                    BrowserNavigationHelper.Back();
            }
            else
            {
                // Successful popstate-initiated navigation. The browser URL may not exactly match the resolved route (e.g. if a redirect occurred), so
                // make sure the URL reflects the current route.
                BrowserNavigationHelper.ReplaceState(_currentSeq, string.Empty, "/" + targetRoute);
            }

            return;
        }

        if (result != NavigationResult.Cancelled)
            return;

        // App-initiated navigation was cancelled. Undo the browser change made in OnNavigationStarting.
        _suppressPopstateCount++;

        switch ((WasmNavState?)state)
        {
            case WasmNavState.PushedNew:
                BrowserNavigationHelper.Back();
                break;
            case WasmNavState.WentBack:
                BrowserNavigationHelper.Forward();
                break;
            case WasmNavState.WentForward:
                BrowserNavigationHelper.Back();
                break;
            default:
                // Replaced/ReplacedNew/null - nothing to undo on the browser side.
                _suppressPopstateCount--;
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnCurrentRouteChanged(NavigatorRoute route)
    {
        // This fires for in-place route mutations (UpdateCurrentRoute, anchor changes) that don't go through NavigateAsyncCore, as well as after every
        // successful navigation. Calling replaceState with the same URL is a no-op for browser history but ensures the address bar stays in sync.
        BrowserNavigationHelper.ReplaceState(_currentSeq, string.Empty, "/" + route);
    }

    private static void OnPopStateRaw(JSObject evt)
    {
        var instance = s_systemNavOwner;

        if (instance is null)
            return;

        double seq;

        try
        {
#pragma warning disable CA1416 // Validate platform compatibility - this entire file is __WASM__-only
            seq = evt.GetTypeOfProperty("state") == "number" ? evt.GetPropertyAsDouble("state") : 0;
#pragma warning restore CA1416
        }
        catch
        {
            seq = 0;
        }

        instance.HandlePopState(seq);
    }

    private void HandlePopState(double newSeq)
    {
        // Restore synchronization context (JS callback loses sync context even though we are on UI thread)

        if (SynchronizationContext.Current is null)
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

        double previousSeq = _currentSeq;
        _currentSeq = newSeq;

        if (_suppressPopstateCount > 0)
        {
            _suppressPopstateCount--;
            return;
        }

        bool isBack = newSeq < previousSeq;
        bool isForward = newSeq > previousSeq;

        if (!isBack && !isForward)
            return;

        bool dispatched = false;

        if (isBack)
        {
            // Mirror HandleSystemBackRequest semantics: a back gesture should consume light-dismiss popups and dismissible dialogs before walking the
            // in-app history. The browser already moved its pointer though, so in any of these consumed-but-no-in-app-nav cases we still need to undo it
            // below to keep the browser stack in sync.

            if (CloseLightDismissPopups())
            {
                // Popup consumed the gesture; dispatched stays false so the browser is rewound.
            }
            else if (IsShowingDialog)
            {
                TryDismissTopDialog();
            }
            else if (!IsNavigating && HasBackHistory)
            {
                _isPopstateNavigation = true;
                TaskRunner.RunAndForget(GoBackAsync());
                dispatched = true;
            }
        }
        else if (!IsNavigating && !IsShowingDialog && HasForwardHistory)
        {
            _isPopstateNavigation = true;
            TaskRunner.RunAndForget(GoForwardAsync());
            dispatched = true;
        }

        if (!dispatched)
        {
            // Either the navigator is busy, a dialog is showing or was just dismissed, a popup was closed, or there's no in-app history in that direction.
            // Undo the browser nav so the browser stays in sync with the in-app state.
            _suppressPopstateCount++;

            if (isBack)
                BrowserNavigationHelper.Forward();
            else
                BrowserNavigationHelper.Back();
        }
    }

    private enum WasmNavState
    {
        None = 0,
        PushedNew,
        ReplacedNew,
        WentBack,
        WentForward,
        Replaced,
    }
}

#endif
