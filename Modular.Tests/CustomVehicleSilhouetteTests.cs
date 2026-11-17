using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleSilhouetteTests
{
    [TestCase("MTNK", false, CustomVehicleSilhouetteKind.Tank)]
    [TestCase("TITN", false, CustomVehicleSilhouetteKind.HeavyWalker)]
    [TestCase("MDRN", false, CustomVehicleSilhouetteKind.MiniDrone)]
    [TestCase("GDRN", false, CustomVehicleSilhouetteKind.MiniTracked)]
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

    [TestCase(false, 4, 6)]
    [TestCase(true, 4, 6)]
    public void TitanPortraitLayoutReconstructsAllStockBlocks(bool heavy, int width, int height)
    {
        var layout = new CustomVehicleSpaceDemo { Silhouette = CustomVehicleSilhouetteKind.HeavyWalker };
        layout.LoadStockConfiguration(heavy);
        Assert.That(layout.HullWidth, Is.EqualTo(width));
        Assert.That(layout.HullHeight, Is.EqualTo(height));
        Assert.That(layout.TurretWidth, Is.EqualTo(5));
        Assert.That(layout.TurretHeight, Is.EqualTo(4));
        Assert.That(layout.Placements.Count, Is.EqualTo(6));
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True, p.ModuleId);
    }

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
            Assert.That(layout.TurretHeight, Is.EqualTo(kind == CustomVehicleSilhouetteKind.HeavyWalker ? 4 : 3));
            Assert.That(layout.HullWidth * layout.HullHeight, Is.EqualTo(kind == CustomVehicleSilhouetteKind.HeavyWalker ? 24 : 32));
            Assert.That(layout.Placements, Is.EqualTo(placements));
        }
    }
}
