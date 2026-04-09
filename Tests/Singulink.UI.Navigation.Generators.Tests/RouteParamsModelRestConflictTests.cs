using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation;

namespace Singulink.UI.Navigation.Generators.Tests;

/// <summary>
/// Verifies that the generator's emission for <see cref="IRouteParamsModel"/> models interacts correctly with the <see cref="RouteValuesCollection"/>
/// reservation rules — specifically that a Rest <see cref="RouteQuery"/> cannot silently shadow a named parameter slot.
/// </summary>
[PrefixTestClass]
public partial class RouteParamsModelRestConflictTests
{
    [RouteParamsModel]
    public partial record ConflictModel
    {
        public required int Id { get; init; }

        public string? Name { get; init; }

        public RouteQuery Rest { get; init; }
    }

    [TestMethod]
    public void NullableWithConflictingRest_Throws()
    {
        // Rest contains a key that matches a nullable named property that is null.
        // Without reservation, the generator would silently let Rest shadow the Name slot on round-trip.
        var model = new ConflictModel { Id = 1, Name = null, Rest = new RouteQuery(("Name", "from-rest")) };

        Should.Throw<ArgumentException>(() => ((IRouteParamsModel)model).ToRouteValues());
    }

    [TestMethod]
    public void NonNullableWithConflictingRest_Throws()
    {
        var model = new ConflictModel { Id = 1, Name = "n", Rest = new RouteQuery(("Id", "2")) };

        Should.Throw<ArgumentException>(() => ((IRouteParamsModel)model).ToRouteValues());
    }

    [TestMethod]
    public void NoConflict_RoundTrips()
    {
        var model = new ConflictModel { Id = 1, Name = null, Rest = new RouteQuery(("extra", "v")) };

        var values = ((IRouteParamsModel)model).ToRouteValues();

        bool ok = TryCreate<ConflictModel>(values, out var result);
        ok.ShouldBeTrue();
        result.ShouldBe(model);
    }

    private static bool TryCreate<T>(RouteValuesCollection values, out T? result) where T : IRouteParamsModel<T>
    {
        bool ok = T.TryCreate(values, out T? value);
        result = value;
        return ok;
    }
}
