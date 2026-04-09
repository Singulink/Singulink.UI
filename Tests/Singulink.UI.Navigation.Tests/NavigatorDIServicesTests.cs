using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorDIServicesTests
{
    [TestMethod]
    public void RootServices_InjectedIntoViewModelConstructor()
    {
        AsyncContextTest.Run(async () =>
        {
            var service = new MyService("hello");
            var sp = new DictionaryServiceProvider { [typeof(MyService)] = service };

            var nav = new TestNavigator(b =>
            {
                b.Services = sp;
                b.MapRoutedView<ConsumerVm, FakeView>();
                b.AddRoute(Route.Build("c").Root<ConsumerVm>());
            });

            await nav.NavigateAsync("c");

            var vm = (ConsumerVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Service.ShouldBeSameAs(service);
        });
    }

    [TestMethod]
    public void RootServices_MissingRequiredService_Throws()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<ConsumerVm, FakeView>();
                b.AddRoute(Route.Build("c").Root<ConsumerVm>());
            });

            await Should.ThrowAsync<InvalidOperationException>(() => nav.NavigateAsync("c"));
        });
    }

    [TestMethod]
    public void RootServices_MissingNullableService_PassedAsNull()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<NullableConsumerVm, FakeView>();
                b.AddRoute(Route.Build("c").Root<NullableConsumerVm>());
            });

            await nav.NavigateAsync("c");
            var vm = (NullableConsumerVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Service.ShouldBeNull();
        });
    }

    [TestMethod]
    public void ChildService_FromParentVm_InjectedIntoChildVm()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<ParentVm, ParentView>();
                b.MapRoutedView<ChildConsumerVm, FakeView>();
                b.AddRoute(Route.Build("p").Root<ParentVm>());
                b.AddRoute(Route.Build("c").Child<ParentVm, ChildConsumerVm>());
            });

            await nav.NavigateAsync("p/c");

            var parentView = (ParentView)nav.RootViewNavigator.ActiveView!;
            var parent = (ParentVm)parentView.DataContext!;
            var child = (ChildConsumerVm)((FakeView)parentView.ChildNavigator.ActiveView!).DataContext!;

            child.ChildSrv.ShouldBeSameAs(parent.ChildSrv);
        });
    }

    [TestMethod]
    public void Navigator_PropertyOnVm_ReturnsOwningNavigator()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<HomeVm, FakeView>();
                b.AddRoute(Route.Build("h").Root<HomeVm>());
            });

            await nav.NavigateAsync("h");
            var vm = (HomeVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Navigator.ShouldBeSameAs(nav);
            vm.TaskRunner.ShouldNotBeNull();
        });
    }

    public class MyService(string value)
    {
        public string Value { get; } = value;
    }

    public class ChildSrvType { }

    public class ConsumerVm(MyService service) : IRoutedViewModel
    {
        public MyService Service { get; } = service;
    }

    public class NullableConsumerVm(MyService? service) : IRoutedViewModel
    {
        public MyService? Service { get; } = service;
    }

    public class HomeVm : IRoutedViewModel { }

    public class ParentVm : IRoutedViewModel
    {
        public ChildSrvType ChildSrv { get; } = new();

        public ParentVm()
        {
            this.SetChildService(ChildSrv);
        }
    }

    public class ChildConsumerVm(ChildSrvType childSrv) : IRoutedViewModel
    {
        public ChildSrvType ChildSrv { get; } = childSrv;
    }

    public class ParentView : FakeParentView { }

    private sealed class DictionaryServiceProvider : Dictionary<Type, object>, IServiceProvider
    {
        public object? GetService(Type serviceType) => TryGetValue(serviceType, out object? value) ? value : null;
    }
}
