using System.ComponentModel;
using Playground.ViewModels.DialogTest;
using Playground.ViewModels.Home;
using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation;

namespace Playground.ViewModels;

[Bindable(true)]
public static class Routes
{
    public static RootRoute<LoginViewModel> LoginRoute { get; } =
        Route.Build("/Login").For<LoginViewModel>();

    public static RootRoute<MainViewModel> MainRoute { get; } =
        Route.Build("/").For<MainViewModel>();

    public static class Main
    {
        public static NestedRoute<MainViewModel, HomeViewModel> HomeRoute { get; } =
            Route.Build("Home").ForNested<MainViewModel, HomeViewModel>();

        public static NestedRoute<MainViewModel, DialogTestViewModel> DialogTestRoute { get; } =
            Route.Build("DialogTest").ForNested<MainViewModel, DialogTestViewModel>();

        public static NestedRoute<MainViewModel, ParamsTestViewModel> ParamsTestRoute { get; } =
            Route.Build("ParamsTest").ForNested<MainViewModel, ParamsTestViewModel>();

        public static NestedRoute<MainViewModel, ShowParamsTestViewModel, (int IntValue, string StringValue)> ShowParamsTestRoute { get; } =
            Route.Build((int i, string s) => $"ParamsTest/{i}/{s}").ForNested<MainViewModel, ShowParamsTestViewModel>();
    }

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRoute(LoginRoute);
        builder.AddRoute(MainRoute);
        builder.AddRoute(Main.HomeRoute);
        builder.AddRoute(Main.DialogTestRoute);
        builder.AddRoute(Main.ParamsTestRoute);
        builder.AddRoute(Main.ShowParamsTestRoute);
    }
}
