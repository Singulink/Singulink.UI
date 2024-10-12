namespace Singulink.UI.Navigation;

internal class ViewInfo(Type viewType, Func<UIElement> createViewFunc)
{
    public Type ViewType { get; } = viewType;

    public UIElement CreateView() => createViewFunc.Invoke();
}
