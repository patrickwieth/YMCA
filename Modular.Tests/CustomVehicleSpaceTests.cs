using NUnit.Framework;
using OpenRA.Mods.CA.Modular;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace Modular.Tests;

public class CustomVehicleSpaceTests
{
    static CustomVehicleSpaceDemo Empty(bool heavy = false)
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetChassis(heavy); return layout;
    }

    [Test]
    public void ChooseChassisBeforeDockingOrInstallingAnything()
    {
        var layout = new CustomVehicleSpaceDemo();
        Assert.That(layout.HasChassis, Is.False);
        Assert.That(layout.HullWidth, Is.Zero);
        Assert.That(layout.Place("engine", false, 1, 1, false), Is.False);
        Assert.Throws<InvalidOperationException>(() => layout.SetTurret(3));
        layout.SetChassis(false);
        Assert.That(layout.Place("weapon", true, 1, 1, false), Is.False);
        layout.SetTurret(3);
        Assert.That(layout.TurretWidth * layout.TurretHeight, Is.EqualTo(12));
        Assert.That(layout.Place("weapon", true, 1, 1, false), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExampleFitsBothChassisAndTheirDedicatedZones(bool heavy)
    {
        var layout = Empty(heavy); layout.LoadExample();
        Assert.That(layout.Heavy, Is.EqualTo(heavy));
        Assert.That(layout.Placements.Count, Is.EqualTo(8));
        Assert.That(layout.Used(false), Is.EqualTo(16));
        Assert.That(layout.Used(true), Is.EqualTo(8));
        Assert.That(layout.HasPdlWithoutBattery, Is.False);
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True);
        layout.LoadExample(); Assert.That(layout.Placements.Count, Is.EqualTo(8));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RunningGearOccupiesFourByOneBottomCellsOnly(bool heavy)
    {
        var layout = Empty(heavy); layout.SetTurret(4);
        Assert.That(CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition("gear"), false), Is.EqualTo((4, 1)));
        Assert.That(layout.Place("gear", false, 1, 1, false), Is.False);
        Assert.That(layout.Place("gear", true, 0, 3, false), Is.False);
        Assert.That(layout.Place("gear", false, 1, layout.HullHeight - 1, true), Is.False);
        Assert.That(layout.Place("gear", false, 1, layout.HullHeight - 1, false), Is.True);
        Assert.That(layout.Place("battery", false, 5, layout.HullHeight - 1, true), Is.False);
    }

    [TestCase("engine")]
    [TestCase("generator")]
    public void MachineryCannotOccupyArmorOrRunningGearZones(string module)
    {
        var layout = Empty(); layout.SetTurret(4);
        Assert.That(layout.Place(module, true, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 2, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 1, false), Is.True);
    }

    [TestCase("armor")]
    [TestCase("reflector")]
    public void ArmorFollowsBlueHullEdgesAndMayRotateVertically(string module)
    {
        var layout = Empty(); layout.SetTurret(3);
        Assert.That(layout.Place(module, false, 1, 1, false), Is.False);
        Assert.That(layout.Place(module, true, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 3, false), Is.False);
        Assert.That(layout.Place(module, false, 1, 0, false), Is.True);
        Assert.That(layout.Place(module, false, 0, 1, true), Is.True);
        Assert.That(layout.Place(module, false, layout.HullWidth - 1, 1, true), Is.True);
    }

    [TestCase(3)]
    [TestCase(4)]
    public void WeaponMustTouchRightFrontDockAndOtherItemsCannotUseIt(int width)
    {
        var layout = Empty(); layout.SetTurret(width);
        Assert.That(layout.Place("weapon", true, 0, 1, false), Is.False);
        Assert.That(layout.Place("weapon", false, 1, 1, false), Is.False);
        Assert.That(layout.Place("ammo", true, width - 1, 0, false), Is.False);
        Assert.That(layout.Place("pdl", true, width - 1, 0, false), Is.False);
        Assert.That(layout.Place("weapon", true, width - 2, 1, false), Is.True);
    }

    [Test]
    public void RotationCollisionAndAtomicMovePreserveOriginalOnFailedDrop()
    {
        var layout = Empty(); layout.SetTurret(3);
        Assert.That(layout.Place("ammo", true, 0, 3, false), Is.False);
        Assert.That(layout.Place("ammo", true, 0, 3, true), Is.True);
        var placed = layout.Placements.Single();
        Assert.That(layout.Place("weapon", true, 1, 2, false), Is.False);
        Assert.That(layout.Place("ammo", true, 1, 3, true, placed.Id), Is.False);
        Assert.That(layout.Placements.Single(), Is.EqualTo(placed));
        Assert.That(layout.Place("ammo", true, 0, 2, true, placed.Id), Is.True);
        Assert.That(layout.Placements.Single().Id, Is.EqualTo(placed.Id));
        Assert.That(layout.At(true, 1, 2)?.Id, Is.EqualTo(placed.Id));
        Assert.That(layout.Place("weapon", true, 1, 0, false, placed.Id), Is.False);
        Assert.That(layout.Place("ammo", false, 1, 1, true, placed.Id), Is.True);
        Assert.That(layout.At(true, 0, 2), Is.Null);
        Assert.That(layout.At(false, 2, 1)?.Id, Is.EqualTo(placed.Id));
    }

    [Test]
    public void MultipleBatteriesAndPdlReflectorRemainAllowed()
    {
        var layout = Empty(); layout.SetTurret(3);
        Assert.That(layout.Place("pdl", true, 0, 0, false), Is.True);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
        Assert.That(layout.Place("battery", false, 1, 1, false), Is.True);
        Assert.That(layout.Place("battery", true, 1, 0, false), Is.True);
        Assert.That(layout.HasPdlWithoutBattery, Is.False);
        Assert.That(layout.Place("reflector", false, 0, 0, false), Is.True);
        foreach (var p in layout.Placements.Where(p => p.ModuleId == "battery").ToArray()) layout.Remove(p.Id);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
    }

    [Test]
    public void ChassisAndTurretChangesResetOnlyTheirOwnDemoState()
    {
        var layout = Empty(); layout.SetTurret(3);
        layout.Place("gear", false, 1, 3, false); layout.Place("weapon", true, 1, 1, false);
        layout.SetTurret(3); Assert.That(layout.Placements.Count, Is.EqualTo(2));
        layout.SetTurret(4); Assert.That(layout.Placements.Single().ModuleId, Is.EqualTo("gear"));
        layout.SetChassis(true); Assert.That(layout.Placements, Is.Empty);
        Assert.That(layout.HullWidth * layout.HullHeight, Is.EqualTo(50));
        Assert.That(layout.TurretWidth, Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.SetTurret(5));
    }

    [Test]
    public void ZoneMapHasBottomPrecedenceAndFrontIsAlwaysRight()
    {
        var layout = Empty(); layout.SetTurret(3);
        Assert.That(layout.ZoneAt(false, 0, 3), Is.EqualTo(Zone.RunningGear));
        Assert.That(layout.ZoneAt(false, 7, 3), Is.EqualTo(Zone.RunningGear));
        Assert.That(layout.ZoneAt(false, 0, 1), Is.EqualTo(Zone.Armor));
        Assert.That(layout.ZoneAt(false, 4, 0), Is.EqualTo(Zone.Armor));
        Assert.That(layout.ZoneAt(false, 1, 1), Is.EqualTo(Zone.Interior));
        Assert.That(layout.ZoneAt(true, 2, 0), Is.EqualTo(Zone.Weapon));
        Assert.That(layout.ZoneAt(true, 0, 0), Is.EqualTo(Zone.Interior));
        Assert.That(layout.ZoneAt(false, -1, 0), Is.EqualTo(Zone.None));
    }

    [Test]
    public void InvalidPlacementDoesNotMutateOrOverflow()
    {
        var layout = Empty();
        Assert.That(layout.Place("battery", false, -1, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, int.MaxValue, int.MaxValue, false), Is.False);
        Assert.That(layout.Place("unknown", false, 0, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, 1, 1, false, 42), Is.False);
        Assert.That(layout.Placements, Is.Empty);
    }
}
