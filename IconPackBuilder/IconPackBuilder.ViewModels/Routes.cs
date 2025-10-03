using Singulink.UI.Navigation;

namespace IconPackBuilder.ViewModels;

public static class Routes
{
    public static RootRoutePart<StartRootModel> StartRoot { get; } =
        Route.Build("/").Root<StartRootModel>();

    public static RootRoutePart<EditorRootModel, string> EditorRoot { get; } =
        Route.Build((string projectFilePath) => $"/Editor/{projectFilePath}").Root<EditorRootModel>();

    public static void AddAllRoutes(this INavigatorBuilder builder)
    {
        builder.AddRouteTo(StartRoot);
        builder.AddRouteTo(EditorRoot);
    }
}
