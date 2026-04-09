using System.ComponentModel;
using Playground.ViewModels.DialogTest;
using Playground.ViewModels.Home;
using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation;

namespace Playground.ViewModels;

[Bindable(true)]
public static class Routes
{
    public static RootRoutePart<LoginViewModel> LoginRoot { get; } =
        Route.Build("/Login").Root<LoginViewModel>();

    public static RootRoutePart<MainViewModel> MainRoot { get; } =
        Route.Build("/").Root<MainViewModel>();

    public static class Main
    {
        public static ChildRoutePart<MainViewModel, HomeViewModel> HomeChild { get; } =
            Route.Build("Home").Child<MainViewModel, HomeViewModel>();

        public static ChildRoutePart<MainViewModel, DialogTestViewModel> DialogTestChild { get; } =
            Route.Build("DialogTest").Child<MainViewModel, DialogTestViewModel>();

        public static ChildRoutePart<MainViewModel, ParamsTestViewModel> ParamsTestChild { get; } =
            Route.Build("ParamsTest").Child<MainViewModel, ParamsTestViewModel>();

        public static ChildRoutePart<MainViewModel, ShowParamsTestViewModel, ShowParamsTestViewModel.Params> ShowParamsTestChild { get; } =
            Route.BuildGroup<ShowParamsTestViewModel.Params>()
                .Add(p => $"ParamsTest/Show/{p.IntValue}")
                .Add(p => $"ParamsTest/Show/{p.IntValue}/{p.StringValue}")
                .Child<MainViewModel, ShowParamsTestViewModel>();
    }

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRoute(LoginRoot);
        builder.AddRoute(MainRoot);
        builder.AddRoute(Main.HomeChild);
        builder.AddRoute(Main.DialogTestChild);
        builder.AddRoute(Main.ParamsTestChild);
        builder.AddRoute(Main.ShowParamsTestChild);
    }
}
