using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.Home;

public partial class HomeViewModel : ObservableObject, IRoutedViewModel
{
    [ObservableProperty]
    public partial string Messages { get; private set; }

    [ObservableProperty]
    public partial bool AllowNavigatingAway { get; set; }

    public HomeViewModel(IMessageProvider messageProvider, MessageContainer messageContainer, MessageContainer2 messageContainer2)
    {
        Messages = $"IMessageProvider: {messageProvider.GetMessage()}\r\n\r\n" +
            $"MessageContainer: {messageContainer.Message}\r\n\r\n" +
            $"MessageContainer2: {messageContainer2.Message}";
    }

    public async Task OnNavigatingAwayAsync(NavigatingArgs args)
    {
        if (!AllowNavigatingAway)
        {
            await Task.Delay(1000); // Simulate some async work
            await this.Navigator.ShowMessageDialogAsync("Navigation cancelled (AllowNavigatingAway is false).");
            args.Cancel = true;
        }
    }
}
