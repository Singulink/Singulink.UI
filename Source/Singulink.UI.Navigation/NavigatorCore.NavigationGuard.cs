using System.Diagnostics;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides the navigation guard implementation for the navigator.
/// </content>
partial class NavigatorCore
{
    private NavigationGuard EnterNavigationGuard(bool blockDialogs) => new NavigationGuard(this, blockDialogs);

    private struct NavigationGuard : IDisposable
    {
        private NavigatorCore? _navigator;

        private readonly bool _wasBlockingNavigation;
        private readonly bool _wasBlockingDialogs;

        public NavigationGuard(NavigatorCore navigator, bool blockDialogs)
        {
            _navigator = navigator;
            _wasBlockingNavigation = navigator._blockNavigation;
            _wasBlockingDialogs = navigator._blockDialogs;

            navigator._blockNavigation = true;
            navigator._blockDialogs = blockDialogs || _wasBlockingDialogs;
        }

        public void Dispose()
        {
            if (_navigator is null)
            {
                Debug.Fail("Navigation guard disposed multiple times.");
                return;
            }

            _navigator._blockNavigation = _wasBlockingNavigation;
            _navigator._blockDialogs = _wasBlockingDialogs;

            _navigator = null;
        }
    }
}
