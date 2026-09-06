using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// Builder for <see cref="TestNavigator"/>. View models are mapped without views, since the test navigator uses placeholders in place of views and
/// dialogs. Routes are added with the same route definitions the application uses.
/// </summary>
public sealed class TestNavigatorBuilder : NavigatorBuilderCore
{
    internal TestNavigatorBuilder()
    {
    }

    /// <inheritdoc/>
    protected override Type RequiredParentViewType => typeof(ITestParentView);

    /// <summary>
    /// Maps a routed view model type so routes can be added for it. View models are created the same way the application navigator creates them,
    /// including constructor injection from <see cref="NavigatorBuilderCore.Services"/> and parent-provided child services.
    /// </summary>
    public void MapViewModel<[DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel>()
        where TViewModel : class, IRoutedViewModelBase
    {
        MapRoutedView(typeof(TViewModel), typeof(TestParentView));
    }

    /// <summary>
    /// Maps a dialog view model type. Mapping dialogs is optional: any dialog view model can be shown without a mapping, and this method only exists to
    /// mirror application builders that register dialogs explicitly.
    /// </summary>
    public void MapDialog<TViewModel>()
        where TViewModel : class, IDialogViewModel
    {
        MapDialog(typeof(TViewModel), static () => new TestDialog());
    }

    /// <inheritdoc/>
    protected override void AddDefaultDialogActivators()
    {
        TryMapDefaultDialog(typeof(MessageDialogViewModel), static () => new TestDialog());
    }
}
