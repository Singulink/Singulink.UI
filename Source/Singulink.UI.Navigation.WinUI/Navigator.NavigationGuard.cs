using System.Diagnostics;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides the navigation guard implementation for the navigator.
/// </content>
partial class Navigator
{
    private NavigationGuard EnterNavigationGuard(bool alsoBlockDialogs)
    {
        if (_blockNavigation)
            throw new UnreachableException("Cannot enter navigation guard while navigation is already blocked.");

        return new NavigationGuard(this, alsoBlockDialogs);
    }

    private struct NavigationGuard : IDisposable
    {
        private readonly Navigator _navigator;

        public NavigationGuard(Navigator navigator, bool blockDialogs)
        {
            _navigator = navigator;
            _navigator._blockNavigation = true;
            _navigator._blockDialogs = blockDialogs;
        }

        public void Dispose()
        {
            _navigator._blockNavigation = false;
            _navigator._blockDialogs = false;
        }
    }
}
