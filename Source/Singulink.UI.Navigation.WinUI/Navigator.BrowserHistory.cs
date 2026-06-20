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

    // The sequence number of the browser history entry that corresponds to the navigator's current in-app route. This is the authoritative target: the
    // browser is kept pointed at this entry. It only changes when a navigation commits or when a self-induced browser move settles.
    private double _committedSeq;

    // True while we are waiting for a popstate caused by our own history.back()/forward() call (an app-initiated move, a cancellation undo, or a rewind of
    // an unhandled user gesture). Such popstates are adopted into _committedSeq and never drive an in-app navigation.
    private bool _selfNavPending;

    // True while an app-initiated Back/Forward navigation is in flight. The actual browser move is deferred until the navigation completes, so
    // OnCurrentRouteChanged must not write the new route's URL onto the entry we are about to leave.
    private bool _appBackForwardInProgress;

    // When an app-initiated Back/Forward move is deferred to completion, this holds the URL to reconcile onto the landed entry once its popstate settles
    // (covers the rare case where a redirect changed the route during the back/forward navigation).
    private string? _pendingReconcileUrl;

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
        _committedSeq = ++_seqCounter;
        BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, BrowserNavigationHelper.GetCurrentUrl());

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
        // If this navigation was triggered by a browser popstate (user clicked the browser/mouse back/forward button), the browser has already moved its
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
                    BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, url);
                    return WasmNavState.ReplacedNew;
                }

                _committedSeq = ++_seqCounter;
                BrowserNavigationHelper.PushState(_committedSeq, string.Empty, url);
                return WasmNavState.PushedNew;

            case NavigationType.Back:
            case NavigationType.Forward:
                // Defer the actual browser move (history.back()/forward()) until the navigation completes successfully. Performing it here would race with
                // the async popstate it produces against the in-app navigation and the OnCurrentRouteChanged URL sync, which is what caused a single
                // app-initiated back to cascade into multiple back navigations when a page completed synchronously. Deferring it to a single call in
                // OnNavigationCompleted means there is exactly one self-induced popstate, issued last, that is reliably recognized via _selfNavPending.
                _appBackForwardInProgress = true;
                return navigationType == NavigationType.Back ? WasmNavState.WentBack : WasmNavState.WentForward;

            case NavigationType.Refresh:
                BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, url);
                return WasmNavState.Replaced;

            default:
                return null;
        }
    }

    /// <inheritdoc />
    protected override void OnNavigationCompleted(NavigationType navigationType, NavigatorRoute targetRoute, NavigationResult result, object? state)
    {
        // Only ever true for an app-initiated Back/Forward; clear it now that the in-app navigation (and its route-changed events) are done.
        _appBackForwardInProgress = false;

        if (_isPopstateNavigation)
        {
            _isPopstateNavigation = false;

            if (result == NavigationResult.Cancelled)
            {
                // The user pressed browser/mouse back/forward, but the in-app navigation was cancelled (e.g. by an "unsaved changes" dialog). The browser
                // already moved its pointer, so undo it to keep browser and in-app state in sync. The undo's popstate is adopted via _selfNavPending.
                _selfNavPending = true;

                if (navigationType == NavigationType.Back)
                    BrowserNavigationHelper.Forward();
                else if (navigationType == NavigationType.Forward)
                    BrowserNavigationHelper.Back();
            }
            else
            {
                // Successful popstate-initiated navigation. The browser is already on the landed entry; make sure its URL reflects the resolved route
                // (it may differ if a redirect occurred).
                BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, "/" + targetRoute);
            }

            return;
        }

        if (result == NavigationResult.Cancelled)
        {
            // App-initiated navigation was cancelled. Undo the only browser change OnNavigationStarting could have made up front: a pushState for a New
            // navigation. Back/Forward defer their browser move to the success path below, so nothing was moved and there is nothing to undo for them.
            if ((WasmNavState?)state == WasmNavState.PushedNew)
            {
                _selfNavPending = true;
                BrowserNavigationHelper.Back();
            }

            return;
        }

        // App-initiated navigation succeeded. Perform the deferred browser move for Back/Forward as a single history call whose popstate is recognized via
        // _selfNavPending. _pendingReconcileUrl fixes the landed entry's URL once the move settles (covers a redirect during the back/forward navigation).
        switch ((WasmNavState?)state)
        {
            case WasmNavState.WentBack:
                _pendingReconcileUrl = "/" + targetRoute;
                _selfNavPending = true;
                BrowserNavigationHelper.Back();
                break;
            case WasmNavState.WentForward:
                _pendingReconcileUrl = "/" + targetRoute;
                _selfNavPending = true;
                BrowserNavigationHelper.Forward();
                break;
            default:
                // New/Refresh already synced their browser state in OnNavigationStarting; nothing to do.
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnCurrentRouteChanged(NavigatorRoute route)
    {
        // While an app-initiated Back/Forward navigation is in flight, the browser move is deferred (see OnNavigationStarting), so the browser is still on
        // the entry we are leaving. Writing the new route's URL here would corrupt that entry, so skip it; the deferred move reconciles the URL instead.
        if (_appBackForwardInProgress)
            return;

        // This fires for in-place route mutations (UpdateCurrentRoute, anchor changes) that don't go through NavigateAsyncCore, as well as after every
        // successful navigation. Calling replaceState with the same URL is a no-op for browser history but ensures the address bar stays in sync.
        BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, "/" + route);
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

        if (_selfNavPending)
        {
            // This popstate was caused by our own history.back()/forward() call (a deferred app-initiated move, a cancellation undo, or a rewind of an
            // unhandled gesture). Adopt the browser's reported position as committed and never drive an in-app navigation from it.
            _selfNavPending = false;
            _committedSeq = newSeq;

            if (_pendingReconcileUrl is not null)
            {
                BrowserNavigationHelper.ReplaceState(_committedSeq, string.Empty, _pendingReconcileUrl);
                _pendingReconcileUrl = null;
            }

            return;
        }

        if (newSeq == _committedSeq)
            return; // Browser is already where the navigator expects it (e.g. a duplicate or redundant popstate); nothing to do.

        // Sequence numbers strictly increase with history-stack position, so a lower number than the committed entry means the user went back and a higher
        // number means the user went forward.
        bool isBack = newSeq < _committedSeq;

        if (isBack)
        {
            // Mirror HandleSystemBackRequest semantics: a back gesture should consume light-dismiss popups and dismissible dialogs before walking the
            // in-app history. The browser already moved its pointer though, so in any consumed-but-no-in-app-nav case we rewind it below to stay in sync.

            if (CloseLightDismissPopups())
            {
                // Popup consumed the gesture; fall through to the rewind.
            }
            else if (IsShowingDialog)
            {
                TryDismissTopDialog();
            }
            else if (!IsNavigating && HasBackHistory)
            {
                _committedSeq = newSeq;
                _isPopstateNavigation = true;
                TaskRunner.RunAndForget(GoBackAsync());
                return;
            }
        }
        else if (!IsNavigating && !IsShowingDialog && HasForwardHistory)
        {
            _committedSeq = newSeq;
            _isPopstateNavigation = true;
            TaskRunner.RunAndForget(GoForwardAsync());
            return;
        }

        // Gesture not honored (navigator busy, dialog showing or just dismissed, a popup was closed, or no in-app history in that direction). Rewind the
        // browser one step back to the committed entry; the resulting popstate is adopted via _selfNavPending.
        _selfNavPending = true;

        if (isBack)
            BrowserNavigationHelper.Forward();
        else
            BrowserNavigationHelper.Back();
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
