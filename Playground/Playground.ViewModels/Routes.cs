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

        public static ChildRoutePart<MainViewModel, ShowParamsTestViewModel, (int IntValue, string StringValue)> ShowParamsTestChild { get; } =
            Route.Build((int i, string s) => $"ParamsTest/{i}/{s}").Child<MainViewModel, ShowParamsTestViewModel>();
    }

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRouteTo(LoginRoot);
        builder.AddRouteTo(MainRoot);
        builder.AddRouteTo(Main.HomeChild);
        builder.AddRouteTo(Main.DialogTestChild);
        builder.AddRouteTo(Main.ParamsTestChild);
        builder.AddRouteTo(Main.ShowParamsTestChild);
    }
}
