using NUnit.Framework;
using OpenRA.Mods.CA.Modular;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace Modular.Tests;

public class CustomVehicleSpaceTests
{
    static CustomVehicleSpaceDemo Empty(bool heavy = false)
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetChassis(heavy); layout.SetTurret(5); return layout;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StockAndPlanningExamplesFitWiderShallowerTurret(bool heavy)
    {
        var layout = Empty(heavy); layout.LoadExample();
        Assert.That(layout.Placements.Count, Is.EqualTo(8));
        Assert.That(layout.Used(true), Is.EqualTo(8));
        Assert.That(layout.TurretWidth, Is.EqualTo(5));
        Assert.That(layout.TurretHeight, Is.EqualTo(3));
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True);
        layout.LoadStockConfiguration(heavy);
        Assert.That(layout.Heavy, Is.EqualTo(heavy));
        Assert.That(layout.Placements.Select(p => p.ModuleId), Is.EquivalentTo(new[] { "engine", "generator", "gear", "armor", "weapon", "ammo" }));
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True);
    }

    [Test]
    public void ChassisBeforeMountingAndOnlyFiveByThreeIsOffered()
    {
        var layout = new CustomVehicleSpaceDemo();
        Assert.That(layout.Place("engine", false, 1, 1, false), Is.False);
        Assert.Throws<InvalidOperationException>(() => layout.SetTurret(5));
        layout.SetChassis(false);
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.False);
        layout.SetTurret(5);
        Assert.That(layout.TurretWidth * layout.TurretHeight, Is.EqualTo(15));
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.SetTurret(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.SetTurret(4));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RunningGearIsFourByOneOnTheBottom(bool heavy)
    {
        var layout = Empty(heavy);
        Assert.That(CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition("gear"), false), Is.EqualTo((4, 1)));
        Assert.That(layout.Place("gear", false, 1, 1, false), Is.False);
        Assert.That(layout.Place("gear", true, 0, 2, false), Is.False);
        Assert.That(layout.Place("gear", false, 1, layout.HullHeight - 1, true), Is.False);
        Assert.That(layout.Place("gear", false, 1, layout.HullHeight - 1, false), Is.True);
    }

    [TestCase("engine")]
    [TestCase("generator")]
    public void GreenMachineryMustFitEntirelyInsideHull(string module)
    {
        var layout = Empty();
        Assert.That(CustomVehicleSpaceDemo.Definition(module).Mount, Is.EqualTo(Zone.Interior));
        Assert.That(layout.Place(module, true, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 2, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 1, false), Is.True);
    }

    [TestCase("armor")]
    [TestCase("reflector")]
    public void ArmorStillUsesOnlyBluePerimeter(string module)
    {
        var layout = Empty();
        Assert.That(layout.Place(module, false, 1, 1, false), Is.False);
        Assert.That(layout.Place(module, true, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 3, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 0, false), Is.True);
        Assert.That(layout.Place(module, false, 0, 1, true), Is.True);
    }

    [Test]
    public void AmmunitionUsesVioletAndWeaponFitsTheExactProtrudingSocket()
    {
        var layout = Empty();
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 5; x++)
                Assert.That(layout.ZoneAt(true, x, y), Is.EqualTo(x == 4 && y == 1 ? Zone.Weapon : Zone.Ammunition));
        Assert.That(layout.Place("ammo", false, 1, 1, false), Is.False);
        Assert.That(layout.Place("ammo", true, 4, 0, false), Is.False);
        Assert.That(layout.Place("ammo", true, 3, 1, true), Is.False);
        Assert.That(layout.Place("ammo", true, 0, 0, false), Is.True);
        Assert.That(layout.Place("weapon", true, 1, 0, false), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.True);
    }

    [TestCase("battery")]
    [TestCase("pdl")]
    public void FreeModulesIgnoreColorsButNotCollisionsOrBounds(string module)
    {
        var layout = Empty();
        Assert.That(layout.Place(module, false, 1, 3, true), Is.True, "yellow");
        Assert.That(layout.Place(module, false, 0, 0, true), Is.True, "blue");
        Assert.That(layout.Place(module, false, 3, 1, false), Is.True, "green");
        Assert.That(layout.Place(module, true, 4, 0, false), Is.True, "red");
        Assert.That(layout.Place(module, true, 0, 0, false), Is.True, "violet");
        Assert.That(layout.Place(module, true, 2, 2, true), Is.True, "bottom turret row");
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.False, "free modules still block weapons");
        Assert.That(layout.Place(module, true, 4, 2, false), Is.False, "bounds");
        Assert.That(layout.Place(module, true, 4, 0, false), Is.False, "overlap");
        var fresh = Empty();
        Assert.That(fresh.Place(module, false, 0, 1, true), Is.True, "may straddle blue and green");
    }

    [TestCase("battery")]
    [TestCase("pdl")]
    public void FreeModulesMayStraddleTheWeaponDockAndVioletCells(string module)
    {
        var layout = Empty();
        Assert.That(layout.Place(module, true, 3, 1, true), Is.True);
        Assert.That(layout.At(true, 4, 1)?.ModuleId, Is.EqualTo(module));
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 2, false), Is.False, "a different row is not a socket");
        layout.Remove(layout.Placements.Single().Id);
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.True);
    }

    [Test]
    public void RotatedAmmunitionFitsLastVioletRowButNotAcrossRed()
    {
        var layout = Empty();
        Assert.That(layout.Place("ammo", true, 2, 2, false), Is.False);
        Assert.That(layout.Place("ammo", true, 2, 2, true), Is.True);
        var original = layout.Placements.Single();
        Assert.That(layout.Place("ammo", true, 3, 1, true, original.Id), Is.False);
        Assert.That(layout.Placements.Single(), Is.EqualTo(original));
    }

    [Test]
    public void MovingAcrossContainersIsAtomicAndCancelable()
    {
        var layout = Empty(); layout.Place("battery", true, 0, 0, false);
        var original = layout.Placements.Single();
        Assert.That(layout.Place("battery", true, 4, 2, false, original.Id), Is.False);
        Assert.That(layout.Placements.Single(), Is.EqualTo(original));
        Assert.That(layout.Place("battery", false, 0, 3, true, original.Id), Is.True);
        Assert.That(layout.At(true, 0, 0), Is.Null);
        Assert.That(layout.At(false, 1, 3)?.Id, Is.EqualTo(original.Id));
        Assert.That(layout.Place("weapon", true, 4, 1, false, original.Id), Is.False);
    }

    [Test]
    public void PdlReflectorAndMultipleBatteriesRemainAllowed()
    {
        var layout = Empty(); layout.Place("pdl", false, 2, 1, false);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
        Assert.That(layout.Place("battery", true, 1, 0, false), Is.True);
        Assert.That(layout.Place("battery", true, 2, 0, false), Is.True);
        Assert.That(layout.Place("reflector", false, 0, 0, false), Is.True);
        Assert.That(layout.HasPdlWithoutBattery, Is.False);
        foreach (var p in layout.Placements.Where(p => p.ModuleId == "battery").ToArray()) layout.Remove(p.Id);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
    }

    [Test]
    public void ResetAndInvalidInputAreSafe()
    {
        var layout = Empty(); layout.LoadStockConfiguration(false);
        layout.SetTurret(5); Assert.That(layout.Placements.Count, Is.EqualTo(6));
        layout.SetTurret(0); Assert.That(layout.Placements.Any(p => p.InTurret), Is.False);
        Assert.That(layout.Placements.Count, Is.EqualTo(4));
        layout.SetChassis(true); Assert.That(layout.Placements, Is.Empty);
        Assert.That(layout.HullWidth * layout.HullHeight, Is.EqualTo(50));
        Assert.That(layout.Place("battery", false, -1, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, int.MaxValue, int.MaxValue, false), Is.False);
        Assert.That(layout.Place("unknown", false, 0, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, 1, 1, false, 42), Is.False);
        Assert.That(layout.ZoneAt(false, 0, 4), Is.EqualTo(Zone.RunningGear));
        Assert.That(layout.ZoneAt(false, -1, 0), Is.EqualTo(Zone.None));
        Assert.That(layout.Placements, Is.Empty);
    }
}
