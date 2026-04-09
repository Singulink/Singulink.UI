using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Verifies the externally observable behavior of <see cref="Route"/> static factories and the route-part hierarchy: string generation,
/// <c>ToConcrete</c> wiring, and round-trip path formatting. Route matching is exercised end-to-end via navigator tests.
/// </summary>
[PrefixTestClass]
public class RouteBuildingTests
{
    [TestMethod]
    public void Build_NoParam_RoundTripsToOriginalRouteString()
    {
        var part = Route.Build("home").Root<NoParamVm>();
        ((IConcreteRootRoutePart)part).Path.ShouldBe("home");
        part.ToString().ShouldBe("home");
    }

    [TestMethod]
    public void Build_NoParam_LiteralRoute_TrimsLeadingSlash()
    {
        var part = Route.Build("/home").Root<NoParamVm>();
        ((IConcreteRootRoutePart)part).Path.ShouldBe("home");
    }

    [TestMethod]
    public void Build_SingleIntParam_ToConcrete_FormatsPath()
    {
        var part = Route.Build<int>(p => $"items/{p}").Root<IntVm>();
        var concrete = part.ToConcrete(42);
        concrete.Path.ShouldBe("items/42");
        concrete.ToString().ShouldBe("items/42");
        concrete.Query.Count.ShouldBe(0);
    }

    [TestMethod]
    public void Build_SingleStringParam_ToConcrete_FormatsPath()
    {
        var part = Route.Build<string>(p => $"users/{p}").Root<StringVm>();
        var concrete = part.ToConcrete("alice");
        concrete.Path.ShouldBe("users/alice");
    }

    [TestMethod]
    public void Build_SingleGuidParam_ToConcrete_FormatsPath()
    {
        var guid = new Guid("11111111-2222-3333-4444-555555555555");
        var part = Route.Build<Guid>(p => $"x/{p}").Root<GuidVm>();
        var concrete = part.ToConcrete(guid);
        concrete.Path.ShouldBe($"x/{guid}");
    }

    [TestMethod]
    public void GetRoute_RootOnly_ProducesPath()
    {
        var part = Route.Build("home").Root<NoParamVm>();
        Route.GetRoute(part).ShouldBe("home");
    }

    [TestMethod]
    public void GetRoute_RootOnly_WithAnchor_AppendsAnchor()
    {
        var part = Route.Build("home").Root<NoParamVm>();
        Route.GetRoute(part, "section1").ShouldBe("home#section1");
    }

    [TestMethod]
    public void GetRoute_RootAndChild_JoinsWithSlash()
    {
        var root = Route.Build("home").Root<NoParamVm>();
        var child = Route.Build("details").Child<NoParamVm, ChildVm>();
        Route.GetRoute(root, child).ShouldBe("home/details");
    }

    [TestMethod]
    public void GetRoute_ParameterizedRoot_FormatsParameter()
    {
        var part = Route.Build<int>(p => $"items/{p}").Root<IntVm>();
        Route.GetRoute(part.ToConcrete(99)).ShouldBe("items/99");
    }

    [TestMethod]
    public void ToConcrete_Equality_SameParam_Equal()
    {
        var part = Route.Build<int>(p => $"items/{p}").Root<IntVm>();
        var c1 = part.ToConcrete(5);
        var c2 = part.ToConcrete(5);
        ((IConcreteRoutePart)c1).Equals(c2).ShouldBeTrue();
    }

    [TestMethod]
    public void ToConcrete_Equality_DifferentParam_NotEqual()
    {
        var part = Route.Build<int>(p => $"items/{p}").Root<IntVm>();
        var c1 = part.ToConcrete(5);
        var c2 = part.ToConcrete(6);
        ((IConcreteRoutePart)c1).Equals(c2).ShouldBeFalse();
    }

    [TestMethod]
    public void Child_SameParentAndChildType_Throws()
    {
        var rb = Route.Build("a");
        Should.Throw<InvalidOperationException>(() => rb.Child<NoParamVm, NoParamVm>());
    }

    [TestMethod]
    public void Build_InvalidInterpolationExpression_AtCompile_StillBuilds()
    {
        // Sanity: build with a string parameter formatted into the path.
        var part = Route.Build<string>(p => $"a/{p}/b").Root<StringVm>();
        var concrete = part.ToConcrete("x");
        concrete.Path.ShouldBe("a/x/b");
    }

    public class NoParamVm : IRoutedViewModel
    {
    }

    public class ChildVm : IRoutedViewModel
    {
    }

    public class IntVm : IRoutedViewModel<int>
    {
    }

    public class StringVm : IRoutedViewModel<string>
    {
    }

    public class GuidVm : IRoutedViewModel<Guid>
    {
    }
}
