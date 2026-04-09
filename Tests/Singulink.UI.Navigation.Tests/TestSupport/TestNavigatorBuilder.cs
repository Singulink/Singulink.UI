using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// Builder for <see cref="TestNavigator"/>. Mirrors the WinUI builder's surface but only requires that parent views implement <see cref="IFakeParentView"/>.
/// </summary>
public sealed class TestNavigatorBuilder : NavigatorBuilderCore
{
    /// <inheritdoc />
    protected override Type RequiredParentViewType => typeof(IFakeParentView);

    /// <summary>
    /// Maps a routed view model type to a view type.
    /// </summary>
    public void MapRoutedView<
        [DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel,
        TView>()
        where TViewModel : class, IRoutedViewModelBase
        where TView : FakeView, new()
    {
        MapRoutedView(typeof(TViewModel), typeof(TView));
    }

    /// <summary>
    /// Maps a dialog view model type to a dialog type.
    /// </summary>
    public void MapDialog<TViewModel, TDialog>()
        where TViewModel : class, IDialogViewModel
        where TDialog : FakeDialog, new()
    {
        MapDialog(typeof(TViewModel), () => new TDialog());
    }

    /// <summary>
    /// Maps a dialog view model type using a custom activator.
    /// </summary>
    public void MapDialog<TViewModel>(Func<object> activator)
        where TViewModel : class, IDialogViewModel
    {
        MapDialog(typeof(TViewModel), activator);
    }

    /// <inheritdoc />
    protected override void AddDefaultDialogActivators()
    {
        TryMapDefaultDialog(typeof(MessageDialogViewModel), () => new FakeMessageDialog());
    }
}
