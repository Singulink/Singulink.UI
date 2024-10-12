namespace Singulink.UI.Navigation;

public class Test
{
    public void TestMain()
    {
        INavigator navigator = new Navigator(App.RootFrame, b => {
            b.MapRoutedView<LoginViewModel, LoginRoot>();
            b.MapRoutedView<MainViewModel, MainRoot>();
            b.MapRoutedView<BrowseFolderViewModel, BrowseFolderPage>();
            b.MapRoutedView<ViewDocumentViewModel, ViewDocumentPage>();
            b.MapRoutedView<ViewPageViewModel, ViewPagePage>();
            b.MapRoutedView<EditDocumentViewModel, EditDocumentPage>();

            b.MapDialog<SelectRepositoryViewModel, Repositories.SelectRepositoryDialog>();

            b.AddRoute(Routes.LoginRoute);
            b.AddRoute(Routes.MainRoute);
            b.AddRoute(Routes.Main.BrowseFolderRoute);
            b.AddRoute(Routes.Main.ViewDocumentRoute);
            b.AddRoute(Routes.Main.ViewPageRoute);
            b.AddRoute(Routes.Main.EditDocumentRoute);
        });

        navigator.NavigateAsync(Routes.LoginRoute);

        navigator.NavigateAsync(Routes.MainRoute.GetSpecified("test-repo"), Routes.Main.ViewPageRoute.GetSpecified((13, 5)));

        navigator.NavigatePartialAsync(Routes.Main.EditDocumentRoute.GetSpecified(123));
    }
}

internal static class Routes
{
    public static readonly Route<LoginViewModel> LoginRoute
        = Route.Build("login").For<LoginViewModel>();

    public static readonly Route<string, MainViewModel> MainRoute
        = Route.Build((string repoSlug) => $"r/{repoSlug}").For<MainViewModel>();

    public static class Main
    {
        public static readonly NestedRoute<MainViewModel, int, BrowseFolderViewModel> BrowseFolderRoute
            = Route.Build((int id) => $"browse/{id}").ForNested<MainViewModel, BrowseFolderViewModel>();

        public static readonly NestedRoute<MainViewModel, int, ViewDocumentViewModel> ViewDocumentRoute
            = Route.Build((int id) => $"doc/{id}").ForNested<MainViewModel, ViewDocumentViewModel>();

        public static readonly NestedRoute<MainViewModel, (int Id, int Page), ViewPageViewModel> ViewPageRoute
            = Route.Build((int id, int page) => $"doc/{id}/page/{page}").ForNested<MainViewModel, ViewPageViewModel>();

        public static readonly NestedRoute<MainViewModel, int, EditDocumentViewModel> EditDocumentRoute
            = Route.Build((int id) => $"doc/{id}/edit").ForNested<MainViewModel, EditDocumentViewModel>();
    }
}

public class LoginViewModel : IRoutedViewModel
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}

public class MainViewModel : IRoutedViewModel<string>
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, string param, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}

public class BrowseFolderViewModel : IRoutedViewModel<int>
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, int param, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}

public class ViewDocumentViewModel : IRoutedViewModel<int>
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, int param, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}

public class ViewPageViewModel : IRoutedViewModel<(int Id, int Page)>
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, (int Id, int Page) param, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}

public class EditDocumentViewModel : IRoutedViewModel<int>
{
    public Task OnNavigatedFrom()
    {
        throw new NotImplementedException();
    }

    public Task OnNavigatedToAsync(INavigator navigator, int param, NavigationArgs args)
    {
        throw new NotImplementedException();
    }
}
