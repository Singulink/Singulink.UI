namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// A plain fake dialog object that mirrors the role of a WinUI <c>ContentDialog</c> in tests.
/// </summary>
public class FakeDialog
{
    public IDialogViewModel? DataContext { get; set; }
}

/// <summary>
/// Default fake dialog used as the built-in activator for <see cref="MessageDialogViewModel"/>.
/// </summary>
public sealed class FakeMessageDialog : FakeDialog
{
}
