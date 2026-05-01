using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public partial class NavigatorRouteQueryTests
{
    [TestMethod]
    public void Navigate_RouteQueryParam_FromUrlString()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, _) = BuildNavWithQueryRoute();

            await nav.NavigateAsync("/search?q=hello&page=2");

            var vm = (SearchVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Query.GetValue<string>("q").ShouldBe("hello");
            vm.Query.GetValue<int>("page").ShouldBe(2);
        });
    }

    [TestMethod]
    public void Navigate_RouteQueryParam_ToConcrete_WithBuilder()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithQueryRoute();

            var query = new RouteQueryBuilder()
                .Add("q", "hello")
                .Add("page", 2)
                .ToQuery();

            await nav.NavigateAsync(route.ToConcrete(query));

            var vm = (SearchVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Query.GetValue<string>("q").ShouldBe("hello");
            vm.Query.GetValue<int>("page").ShouldBe(2);

            string url = nav.CurrentRoute.ToString();
            url.ShouldContain("search");
            url.ShouldContain("q=hello");
            url.ShouldContain("page=2");
        });
    }

    [TestMethod]
    public void Navigate_RouteQueryParam_ToConcrete_WithDirectQuery()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithQueryRoute();

            var query = new RouteQuery(("q", "abc"), ("count", "5"));

            await nav.NavigateAsync(route.ToConcrete(query));

            var vm = (SearchVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Query.GetValue<string>("q").ShouldBe("abc");
            vm.Query.GetValue<int>("count").ShouldBe(5);
        });
    }

    [TestMethod]
    public void Navigate_RouteQueryParam_ToConcrete_EmptyQuery()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithQueryRoute();

            await nav.NavigateAsync(route.ToConcrete(RouteQuery.Empty));

            var vm = (SearchVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Query.Count.ShouldBe(0);
            nav.CurrentRoute.ToString().ShouldNotContain("?");
        });
    }

    [TestMethod]
    public void Navigate_ParamsModelWithQueryProperty_ToConcrete_QueryValuesPreserved()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithParamsModelRoute();

            var query = new RouteQueryBuilder()
                .Add("filter", "active")
                .Add("page", 3)
                .ToQuery();

            var concrete = route.ToConcrete(new SearchParamsVm.Params
            {
                IntValue = 42,
                Rest = query,
            });

            await nav.NavigateAsync(concrete);

            var vm = (SearchParamsVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.IntValue.ShouldBe(42);
            vm.Rest.GetValue<string>("filter").ShouldBe("active");
            vm.Rest.GetValue<int>("page").ShouldBe(3);
        });
    }

    [TestMethod]
    public void Navigate_ParamsModelWithQueryProperty_ToConcrete_RoundTripsThroughString()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithParamsModelRoute();

            var concrete = route.ToConcrete(new SearchParamsVm.Params
            {
                IntValue = 7,
                Rest = new RouteQuery(("a", "1"), ("b", "two")),
            });

            await nav.NavigateAsync(concrete);

            string url = nav.CurrentRoute.ToString();
            url.ShouldContain("show/7");
            url.ShouldContain("a=1");
            url.ShouldContain("b=two");
        });
    }

    [TestMethod]
    public void Navigate_ParamsModelWithQueryProperty_ToConcrete_QueryValuesDoNotShadowDeclaredProperties()
    {
        AsyncContextTest.Run(async () =>
        {
            var (nav, route) = BuildNavWithParamsModelRoute();

            // Declared properties on the params model take precedence; values for "IntValue" placed in the
            // RouteQuery property should not shadow the actual IntValue path hole.

            var concrete = route.ToConcrete(new SearchParamsVm.Params
            {
                IntValue = 100,
                Rest = new RouteQuery(("extra", "x")),
            });

            await nav.NavigateAsync(concrete);

            var vm = (SearchParamsVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.IntValue.ShouldBe(100);
            vm.Rest.GetValue<string>("extra").ShouldBe("x");
            vm.Rest.ContainsKey("IntValue").ShouldBeFalse();
        });
    }

    private static (TestNavigator Nav, RootRoutePart<SearchVm, RouteQuery> Route) BuildNavWithQueryRoute()
    {
        var route = Route.Build<RouteQuery>("/search").Root<SearchVm>();
        var nav = new TestNavigator(b =>
        {
            b.MapRoutedView<SearchVm, FakeView>();
            b.AddRoute(route);
        });
        return (nav, route);
    }

    private static (TestNavigator Nav, RootRoutePart<SearchParamsVm, SearchParamsVm.Params> Route) BuildNavWithParamsModelRoute()
    {
        var route = Route.Build<SearchParamsVm.Params>(p => $"show/{p.IntValue}").Root<SearchParamsVm>();
        var nav = new TestNavigator(b =>
        {
            b.MapRoutedView<SearchParamsVm, FakeView>();
            b.AddRoute(route);
        });
        return (nav, route);
    }

    public partial class SearchVm : IRoutedViewModel<RouteQuery>
    {
        public RouteQuery Query => this.Parameter;
    }

    public partial class SearchParamsVm : IRoutedViewModel<SearchParamsVm.Params>
    {
        [RouteParamsModel]
        public partial record Params
        {
            public required int IntValue { get; init; }

            public RouteQuery Rest { get; init; }
        }

        public int IntValue => this.Parameter.IntValue;

        public RouteQuery Rest => this.Parameter.Rest;
    }
}
