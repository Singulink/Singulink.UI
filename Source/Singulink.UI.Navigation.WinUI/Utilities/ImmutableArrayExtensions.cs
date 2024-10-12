using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation.Utilities;

internal static class ImmutableArrayExtensions
{
    public static int FindIndex<T>(this ImmutableArray<T> array, Predicate<T> match) => Array.FindIndex(array.AsArrayUnsafe(), match);

    public static int FindLastIndex<T>(this ImmutableArray<T> array, Predicate<T> match) => Array.FindLastIndex(array.AsArrayUnsafe(), match);

    private static T[] AsArrayUnsafe<T>(this ImmutableArray<T> array) => Unsafe.As<ImmutableArray<T>, T[]>(ref array);
}
