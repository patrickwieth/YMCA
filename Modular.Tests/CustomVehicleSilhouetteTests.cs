using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleSilhouetteTests
{
    [TestCase("MTNK", false, CustomVehicleSilhouetteKind.Tank)]
    [TestCase("TITN", false, CustomVehicleSilhouetteKind.Walker)]
    [TestCase("Juggernaut", false, CustomVehicleSilhouetteKind.Walker)]
    [TestCase("MAMMOTHMK2", false, CustomVehicleSilhouetteKind.Walker)]
    [TestCase("GUNW", false, CustomVehicleSilhouetteKind.Walker)]
    [TestCase("TPOD", false, CustomVehicleSilhouetteKind.Tripod)]
    [TestCase("Hexapod", false, CustomVehicleSilhouetteKind.Tripod)]
    [TestCase("HMMV", false, CustomVehicleSilhouetteKind.LightVehicle)]
    [TestCase("BGGY", false, CustomVehicleSilhouetteKind.LightVehicle)]
    [TestCase("JEEP", false, CustomVehicleSilhouetteKind.LightVehicle)]
    [TestCase("BIKE", false, CustomVehicleSilhouetteKind.Bike)]
    [TestCase("BTR", false, CustomVehicleSilhouetteKind.Wheeled)]
    [TestCase("SEEK", true, CustomVehicleSilhouetteKind.Hover)]
    public void NativeActorSelectsPresentation(string actor, bool hover, CustomVehicleSilhouetteKind expected)
        => Assert.That(CustomVehicleSilhouette.ForActor(actor, hover), Is.EqualTo(expected));

    [Test]
    public void PresentationDoesNotChangePlacementBudgets()
    {
        var layout = new CustomVehicleSpaceDemo();
        layout.LoadStockConfiguration(false);
        var placements = layout.Placements.ToArray();
        foreach (var kind in Enum.GetValues<CustomVehicleSilhouetteKind>())
        {
            layout.Silhouette = kind;
            Assert.That(layout.TurretWidth, Is.EqualTo(5));
            Assert.That(layout.TurretHeight, Is.EqualTo(3));
            Assert.That(layout.HullWidth, Is.EqualTo(8));
            Assert.That(layout.Placements, Is.EqualTo(placements));
        }
    }
}
