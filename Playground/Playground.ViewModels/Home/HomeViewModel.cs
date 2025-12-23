using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.Home;

public partial class HomeViewModel : ObservableObject, IRoutedViewModel
{
    [ObservableProperty]
    public partial string Messages { get; private set; }

    public HomeViewModel(IMessageProvider messageProvider, MessageContainer messageContainer, MessageContainer2 messageContainer2)
    {
        Messages = $"IMessageProvider: {messageProvider.GetMessage()}\r\n\r\n" +
            $"MessageContainer: {messageContainer.Message}\r\n\r\n" +
            $"MessageContainer2: {messageContainer2.Message}";
    }
}
