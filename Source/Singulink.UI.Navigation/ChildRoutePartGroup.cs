using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

internal class ChildRoutePartGroup<TParentViewModel, TChildViewModel, [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] TParam> : ChildRoutePart<TParentViewModel, TChildViewModel, TParam>
    where TParentViewModel : class
    where TChildViewModel : class, IRoutedViewModel<TParam>
    where TParam : notnull
{
    private readonly ImmutableArray<DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam>> _candidates;
    private readonly ImmutableArray<DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam>> _candidatesByHoleCountDesc;

    internal ChildRoutePartGroup(IEnumerable<DirectChildRoutePart<TParentViewModel, TChildViewModel, TParam>> candidates)
    {
        _candidates = [.. candidates];
        _candidatesByHoleCountDesc = [.. _candidates.OrderByDescending(c => c.RouteBuilder.HoleNames.Count)];
    }

    public override IConcreteChildRoutePart<TParentViewModel, TChildViewModel> ToConcrete(TParam parameter)
    {
        var values = RouteParamsHandler<TParam>.Instance.ToRouteValues(parameter);

        foreach (var candidate in _candidatesByHoleCountDesc)
        {
            if (candidate.RouteBuilder.AreAllHolesSatisfied(values))
                return candidate.ToConcrete(parameter, values);
        }

        throw new InvalidOperationException($"No route part in the group could satisfy the parameter '{parameter}'.");
    }

    internal override IEnumerable<RoutePart> GetRegistrationParts() => _candidates;

    internal override bool TryMatch(ReadOnlySpan<char> routeString, RouteQuery query, [MaybeNullWhen(false)] out IConcreteRoutePart concreteRoute, out ReadOnlySpan<char> rest)
    {
        throw new UnreachableException("Unexpected invocation of unsupported method.");
    }
}
