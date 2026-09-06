using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Icons;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.IconsTest;

public partial class IconsTestViewModel : ObservableObject, IRoutedViewModel
{
    /// <summary>
    /// Gets the Seagull "Arrow Previous" icon, which has a distinct RTL glyph. Used to test runtime (non-compiled) bindings.
    /// </summary>
    public IIconGlyph Icon { get; } = new IconWithRtlGlyph(0xF0050, 0x100050);
}
