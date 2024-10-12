namespace Singulink.UI.Navigation;

internal class RouteHole(string name, Type holeType)
{
    public string Name => name;

    public Type HoleType { get; } = holeType;
}
