using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleCanvasGeometryTests
{
    [Test]
    public void DisplayScaleDoesNotChangeInventoryCapacity()
    {
        Assert.That(CustomVehicleCanvasGeometry.Cell, Is.EqualTo(64));
        Assert.That(CustomVehicleCanvasGeometry.Scale(28), Is.EqualTo(64));
        Assert.That(CustomVehicleCanvasGeometry.Scale(56), Is.EqualTo(128));
        var layout = new CustomVehicleSpaceDemo { WeaponKind = CustomWeaponKind.BattleTank120mm, AmmunitionId = "medium-tank-shell" };
        layout.LoadStockConfiguration(false);
        Assert.That(layout.HullWidth, Is.EqualTo(8));
        Assert.That(layout.HullHeight, Is.EqualTo(4));
        Assert.That(layout.TurretWidth, Is.EqualTo(5));
        Assert.That(layout.TurretHeight, Is.EqualTo(3));
        Assert.That(layout.WeaponSocket.Width, Is.EqualTo(4));
        Assert.That(layout.WeaponFootprint.Width, Is.EqualTo(5));
        Assert.That(layout.ModuleFor("ammo").Width * CustomVehicleCanvasGeometry.Cell, Is.EqualTo(128));
    }

    [TestCase(1280, 720)]
    [TestCase(1920, 1080)]
    [TestCase(2560, 1440)]
    [TestCase(3840, 2160)]
    public void EveryCellIncludingExternalSocketsIsReachableWithoutShrinking(int screenWidth, int screenHeight)
    {
        var width = screenWidth - 32 - 560;
        var height = screenHeight - 32 - 330;
        const int cell = CustomVehicleCanvasGeometry.Cell;
        foreach (var kind in Enum.GetValues<CustomVehicleSilhouetteKind>())
        foreach (var weapon in new[] { CustomWeaponKind.Cannon, CustomWeaponKind.BattleTank120mm, CustomWeaponKind.SonicEmitter })
        {
            var layout = new CustomVehicleSpaceDemo { Silhouette = kind, WeaponKind = weapon };
            layout.LoadStockConfiguration(true);
            var hull = CustomVehicleCanvasGeometry.HullOrigin(layout);
            var turret = CustomVehicleCanvasGeometry.TurretOrigin(layout);
            Assert.That(hull.Y, Is.GreaterThan(turret.Y + layout.TurretHeight * cell), "containers must not overlap");
            foreach (var inTurret in new[] { false, true })
            for (var y = 0; y < 6; y++)
            for (var x = 0; x < 10; x++)
            {
                if (!layout.ContainsCell(inTurret, x, y)) continue;
                var origin = inTurret ? turret : hull;
                var px = origin.X + x * cell;
                var py = origin.Y + y * cell;
                var pan = CustomVehicleCanvasGeometry.ClampPan(layout, width, height, px, py - 24);
                Assert.That(px - pan.X, Is.GreaterThanOrEqualTo(0));
                Assert.That(py - pan.Y, Is.GreaterThanOrEqualTo(24));
                Assert.That(px + cell - pan.X, Is.LessThanOrEqualTo(width));
                Assert.That(py + cell - pan.Y, Is.LessThanOrEqualTo(height));
                // Panning is a translation: the same cell is hit after converting back.
                Assert.That((px - pan.X + pan.X - origin.X) / cell, Is.EqualTo(x));
                Assert.That((py - pan.Y + pan.Y - origin.Y) / cell, Is.EqualTo(y));
            }
            Assert.That(CustomVehicleCanvasGeometry.ClampPan(layout, width, height, -999, -999), Is.EqualTo((0, 0)));
        }
    }
}
