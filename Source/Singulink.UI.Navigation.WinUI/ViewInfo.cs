namespace Singulink.UI.Navigation.WinUI;

internal class ViewInfo(Type viewType, Func<UIElement> viewFactory)
{
    public Type ViewType { get; } = viewType;

    public UIElement CreateView() => viewFactory.Invoke();
}
