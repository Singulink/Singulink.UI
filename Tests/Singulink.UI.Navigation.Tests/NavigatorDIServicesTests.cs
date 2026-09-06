using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorDIServicesTests
{
    [TestMethod]
    public void RootServices_InjectedIntoViewModelConstructor()
    {
        NavigationTestContext.Run(async () =>
        {
            var service = new MyService("hello");
            var sp = new DictionaryServiceProvider { [typeof(MyService)] = service };

            var nav = new TestNavigator(b =>
            {
                b.Services = sp;
                b.MapViewModel<ConsumerVm>();
                b.AddRoute(Route.Build("c").Root<ConsumerVm>());
            });

            await nav.NavigateAsync("c");

            var vm = nav.ActiveViewModel<ConsumerVm>();
            vm.Service.ShouldBeSameAs(service);
        });
    }

    [TestMethod]
    public void RootServices_MissingRequiredService_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<ConsumerVm>();
                b.AddRoute(Route.Build("c").Root<ConsumerVm>());
            });

            await Should.ThrowAsync<InvalidOperationException>(() => nav.NavigateAsync("c"));
        });
    }

    [TestMethod]
    public void RootServices_MissingNullableService_PassedAsNull()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<NullableConsumerVm>();
                b.AddRoute(Route.Build("c").Root<NullableConsumerVm>());
            });

            await nav.NavigateAsync("c");
            var vm = nav.ActiveViewModel<NullableConsumerVm>();
            vm.Service.ShouldBeNull();
        });
    }

    [TestMethod]
    public void ChildService_FromParentVm_InjectedIntoChildVm()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<ParentVm>();
                b.MapViewModel<ChildConsumerVm>();
                b.AddRoute(Route.Build("p").Root<ParentVm>());
                b.AddRoute(Route.Build("c").Child<ParentVm, ChildConsumerVm>());
            });

            await nav.NavigateAsync("p/c");

            var parent = nav.ActiveViewModel<ParentVm>();
            var child = nav.ActiveViewModel<ChildConsumerVm>();

            child.ChildSrv.ShouldBeSameAs(parent.ChildSrv);
        });
    }

    [TestMethod]
    public void Navigator_PropertyOnVm_ReturnsOwningNavigator()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<HomeVm>();
                b.AddRoute(Route.Build("h").Root<HomeVm>());
            });

            await nav.NavigateAsync("h");
            var vm = nav.ActiveViewModel<HomeVm>();
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


    private sealed class DictionaryServiceProvider : Dictionary<Type, object>, IServiceProvider
    {
        public object? GetService(Type serviceType) => TryGetValue(serviceType, out object? value) ? value : null;
    }
}
