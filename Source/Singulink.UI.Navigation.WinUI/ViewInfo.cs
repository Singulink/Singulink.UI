namespace Singulink.UI.Navigation.WinUI;

internal class ViewInfo(Type viewType, Func<UIElement> createViewFunc)
{
    public Type ViewType { get; } = viewType;

    public UIElement CreateView() => createViewFunc.Invoke();
}
