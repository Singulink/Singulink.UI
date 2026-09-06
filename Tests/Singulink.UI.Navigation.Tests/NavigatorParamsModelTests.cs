using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public partial class NavigatorParamsModelTests
{
    [TestMethod]
    public void Navigate_ParamsModel_ParsesPathAndQuery()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _) = BuildNavWithRoute();

            await nav.NavigateAsync("show/42?StringValue=hello");

            var vm = nav.ActiveViewModel<ShowVm>();
            vm.IntValue.ShouldBe(42);
            vm.StringValue.ShouldBe("hello");
        });
    }

    [TestMethod]
    public void Navigate_ParamsModel_NoOptional()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _) = BuildNavWithRoute();
            await nav.NavigateAsync("show/7");

            var vm = nav.ActiveViewModel<ShowVm>();
            vm.IntValue.ShouldBe(7);
            vm.StringValue.ShouldBeNull();
        });
    }

    [TestMethod]
    public void ToConcrete_ParamsModel_RoundTripsThroughString()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, route) = BuildNavWithRoute();
            var concrete = route.ToConcrete(new ShowVm.Params { IntValue = 9, StringValue = "abc" });

            await nav.NavigateAsync(concrete);

            nav.CurrentRoute.ToString().ShouldContain("show/9");
            nav.CurrentRoute.ToString().ShouldContain("StringValue=abc");
        });
    }

    [TestMethod]
    public void Navigate_ParamsModel_RemainingQuery_Captured()
    {
        NavigationTestContext.Run(async () =>
        {
            var (nav, _) = BuildNavWithRoute();
            await nav.NavigateAsync("show/3?StringValue=x&extra=1&also=2");

            var vm = nav.ActiveViewModel<ShowVm>();
            vm.Rest.ContainsKey("extra").ShouldBeTrue();
            vm.Rest.ContainsKey("also").ShouldBeTrue();
            vm.Rest.ContainsKey("StringValue").ShouldBeFalse();
        });
    }

    [TestMethod]
    public void RouteGroup_ParamsModel_PicksMostSatisfiedPattern()
    {
        NavigationTestContext.Run(async () =>
        {
            var route = Route.BuildGroup<ShowVm.Params>()
                .Add(p => $"show/{p.IntValue}")
                .Add(p => $"show/{p.IntValue}/{p.StringValue}")
                .Root<ShowVm>();

            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<ShowVm>();
                b.AddRoute(route);
            });

            // Without StringValue, picks the shorter pattern.
            var c1 = route.ToConcrete(new ShowVm.Params { IntValue = 5 });
            await nav.NavigateAsync(c1);
            nav.CurrentRoute.ToString().ShouldBe("show/5");

            // With StringValue, picks the longer pattern.
            var c2 = route.ToConcrete(new ShowVm.Params { IntValue = 5, StringValue = "abc" });
            await nav.NavigateAsync(c2);
            nav.CurrentRoute.ToString().ShouldBe("show/5/abc");
        });
    }

    private static (TestNavigator Nav, RootRoutePart<ShowVm, ShowVm.Params> Route) BuildNavWithRoute()
    {
        var route = Route.Build<ShowVm.Params>(p => $"show/{p.IntValue}").Root<ShowVm>();
        var nav = new TestNavigator(b =>
        {
            b.MapViewModel<ShowVm>();
            b.AddRoute(route);
        });
        return (nav, route);
    }

    public partial class ShowVm : IRoutedViewModel<ShowVm.Params>
    {
        [RouteParamsModel]
        public partial record Params
        {
            public required int IntValue { get; init; }

            public string? StringValue { get; init; }

            public RouteQuery Rest { get; init; }
        }

        public int IntValue => this.Parameter.IntValue;

        public string? StringValue => this.Parameter.StringValue;

        public RouteQuery Rest => this.Parameter.Rest;
    }
}
