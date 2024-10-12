using System.Globalization;

namespace Singulink.UI.Navigation;

internal ref struct InvariantCultureContext
{
    private readonly CultureInfo _savedCulture;

    public InvariantCultureContext()
    {
        _savedCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose() => CultureInfo.CurrentCulture = _savedCulture;
}
