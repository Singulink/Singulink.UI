using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a builder used for constructing routes without parameters.
/// </summary>
public class RouteBuilder
{
    private readonly string _route;

    internal RouteBuilder(string? route) => _route = route ?? string.Empty;

    /// <summary>
    /// Creates a root route part for the specified view model type.
    /// </summary>
    public RootRoutePart<TViewModel> Root<TViewModel>()
        where TViewModel : class, IRoutedViewModel
    {
        return new RootRoutePart<TViewModel>(this);
    }

    /// <summary>
    /// Creates a child route part for the specified parent and child view model type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Parent and child view models were the same type.</exception>
    public ChildRoutePart<TParentViewModel, TChildViewModel> Child<TParentViewModel, TChildViewModel>()
        where TParentViewModel : class
        where TChildViewModel : class, IRoutedViewModel
    {
        if (typeof(TParentViewModel) == typeof(TChildViewModel))
            throw new InvalidOperationException("Parent and child view models cannot be the same type.");

        return new ChildRoutePart<TParentViewModel, TChildViewModel>(this);
    }

    internal string GetPartPath() => _route;

    internal bool TryMatch(ReadOnlySpan<char> route, out ReadOnlySpan<char> rest)
    {
        rest = RouteBuilderBase.PreProcessRouteString(route);

        if (!rest.StartsWith(_route, StringComparison.Ordinal))
            return false;

        rest = rest[_route.Length..];
        return true;
    }
}

/// <summary>
/// Represents a builder used for constructing parameterized routes.
/// </summary>
public abstract class RouteBuilder<TParam> : RouteBuilderBase
    where TParam : notnull
{
    internal RouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    /// <summary>
    /// Creates a root route part for the specified view model type.
    /// </summary>
    public RootRoutePart<TViewModel, TParam> Root<TViewModel>()
        where TViewModel : class, IRoutedViewModel<TParam>
    {
        return new RootRoutePart<TViewModel, TParam>(this);
    }

    /// <summary>
    /// Creates a child route part for the specified parent and child view model type.
    /// </summary>
    public ChildRoutePart<TParentViewModel, TChildViewModel, TParam> Child<TParentViewModel, TChildViewModel>()
        where TParentViewModel : class
        where TChildViewModel : class, IRoutedViewModel<TParam>
    {
        return new ChildRoutePart<TParentViewModel, TChildViewModel, TParam>(this);
    }

    internal abstract string GetPartPath(TParam parameter);

    internal abstract bool TryMatch(ReadOnlySpan<char> route, [MaybeNullWhen(false)] out TParam parameter, out ReadOnlySpan<char> rest);
}

internal class SingleParamRouteBuilder<T> : RouteBuilder<T>
    where T : notnull, IParsable<T>, IEquatable<T>
{
    internal SingleParamRouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    internal override string GetPartPath(T parameter)
    {
        var sb = new StringBuilder();
        int index = 0;

        using (new InvariantCultureContext())
        {
            AddIfLiteralThenAddHole(ref index, parameter, sb);
            AddIfLiteral(ref index, sb);
        }

        EnsurePartIndexAtEnd(index);
        return sb.ToString();
    }

    internal override bool TryMatch(ReadOnlySpan<char> route, [MaybeNullWhen(false)] out T parameter, out ReadOnlySpan<char> rest)
    {
        rest = PreProcessRouteString(route);
        int index = 0;

        using (new InvariantCultureContext())
        {
            if (!MatchIfLiteralThenMatchHole(ref index, ref rest, out parameter))
                return false;

            if (!MatchIfLiteral(ref index, ref rest))
                return false;
        }

        EnsurePartIndexAtEnd(index);
        return true;
    }
}

internal class TupleRouteBuilder<T1, T2> : RouteBuilder<(T1 Param1, T2 Param2)>
    where T1 : notnull, IParsable<T1>, IEquatable<T1>
    where T2 : notnull, IParsable<T2>, IEquatable<T2>
{
    internal TupleRouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    internal override string GetPartPath((T1 Param1, T2 Param2) p)
    {
        var sb = new StringBuilder();
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            AddIfLiteralThenAddHole(ref partIndex, p.Param1, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param2, sb);
            AddIfLiteral(ref partIndex, sb);
        }

        EnsurePartIndexAtEnd(partIndex);
        return sb.ToString();
    }

    internal override bool TryMatch(ReadOnlySpan<char> route, out (T1 Param1, T2 Param2) p, out ReadOnlySpan<char> rest)
    {
        p = default;

        rest = PreProcessRouteString(route);
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param1!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param2!))
                return false;

            if (!MatchIfLiteral(ref partIndex, ref rest))
                return false;
        }

        EnsurePartIndexAtEnd(partIndex);
        return true;
    }
}

internal class TupleRouteBuilder<T1, T2, T3> : RouteBuilder<(T1 Param1, T2 Param2, T3 Param3)>
    where T1 : notnull, IParsable<T1>, IEquatable<T1>
    where T2 : notnull, IParsable<T2>, IEquatable<T2>
    where T3 : notnull, IParsable<T3>, IEquatable<T3>
{
    internal TupleRouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    internal override string GetPartPath((T1 Param1, T2 Param2, T3 Param3) p)
    {
        var sb = new StringBuilder();
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            AddIfLiteralThenAddHole(ref partIndex, p.Param1, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param2, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param3, sb);
            AddIfLiteral(ref partIndex, sb);
        }

        EnsurePartIndexAtEnd(partIndex);
        return sb.ToString();
    }

    internal override bool TryMatch(ReadOnlySpan<char> route, out (T1 Param1, T2 Param2, T3 Param3) p, out ReadOnlySpan<char> rest)
    {
        p = default;

        rest = PreProcessRouteString(route);
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param1!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param2!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param3!))
                return false;

            if (!MatchIfLiteral(ref partIndex, ref rest))
                return false;
        }

        EnsurePartIndexAtEnd(partIndex);
        return true;
    }
}

internal class TupleRouteBuilder<T1, T2, T3, T4> : RouteBuilder<(T1 Param1, T2 Param2, T3 Param3, T4 Param4)>
    where T1 : notnull, IParsable<T1>, IEquatable<T1>
    where T2 : notnull, IParsable<T2>, IEquatable<T2>
    where T3 : notnull, IParsable<T3>, IEquatable<T3>
    where T4 : notnull, IParsable<T4>, IEquatable<T4>
{
    internal TupleRouteBuilder(IEnumerable<object> routeParts) : base(routeParts) { }

    internal override string GetPartPath((T1 Param1, T2 Param2, T3 Param3, T4 Param4) p)
    {
        var sb = new StringBuilder();
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            AddIfLiteralThenAddHole(ref partIndex, p.Param1, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param2, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param3, sb);
            AddIfLiteralThenAddHole(ref partIndex, p.Param4, sb);
            AddIfLiteral(ref partIndex, sb);
        }

        EnsurePartIndexAtEnd(partIndex);
        return sb.ToString();
    }

    internal override bool TryMatch(ReadOnlySpan<char> route, [MaybeNullWhen(false)] out (T1 Param1, T2 Param2, T3 Param3, T4 Param4) p, out ReadOnlySpan<char> rest)
    {
        p = default;

        rest = PreProcessRouteString(route);
        int partIndex = 0;

        using (new InvariantCultureContext())
        {
            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param1!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param2!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param3!))
                return false;

            if (!MatchIfLiteralThenMatchHole(ref partIndex, ref rest, out p.Param4!))
                return false;

            if (!MatchIfLiteral(ref partIndex, ref rest))
                return false;
        }

        EnsurePartIndexAtEnd(partIndex);
        return true;
    }
}
