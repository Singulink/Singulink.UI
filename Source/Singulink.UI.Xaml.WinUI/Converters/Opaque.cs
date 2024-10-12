namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods to opacity values (<see cref="double"/>) for use in XAML bindings.
/// </summary>
public static class Opaque
{
    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="true"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfTrue(bool? value) => value is true ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="true"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfTrue(bool value) => value ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="true"/> or <see langword="null"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfTrueOrNull(bool? value) => value is not false ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="false"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfFalse(bool? value) => value is false ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="false"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfFalse(bool value) => !value ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="false"/> or <see langword="null"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfFalseOrNull(bool? value) => value is not true ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified focus state is in any state other than <see cref="FocusState.Unfocused"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfFocused(FocusState focusState) => focusState is not FocusState.Unfocused ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <c>0</c>.
    /// </summary>
    public static double IfUnfocused(FocusState focusState) => focusState is FocusState.Unfocused ? 1 : 0;
}
