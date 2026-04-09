using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation.WinUI;

#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <inheritdoc cref="INavigatorBuilder" />
public class NavigatorBuilder : NavigatorBuilderCore
{
    internal NavigatorBuilder() { }

    /// <inheritdoc/>
    protected override Type RequiredParentViewType => typeof(IParentView);

    /// <summary>
    /// Maps a routed view model to a routed view.
    /// </summary>
    public void MapRoutedView<
        [DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel,
        TView>()
        where TViewModel : class, IRoutedViewModelBase
        where TView : FrameworkElement, new()
    {
        MapRoutedView(typeof(TViewModel), typeof(TView));
    }

    /// <summary>
    /// Maps a view model to a dialog.
    /// </summary>
    public void MapDialog<TViewModel, TDialog>()
        where TViewModel : class, IDialogViewModel
        where TDialog : ContentDialog, new()
    {
        MapDialog(typeof(TViewModel), () => new TDialog());
    }

    /// <inheritdoc/>
    protected override void AddDefaultDialogActivators()
    {
        TryMapDefaultDialog(typeof(MessageDialogViewModel), () => new MessageDialog());
    }
}
