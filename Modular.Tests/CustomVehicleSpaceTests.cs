using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleSpaceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ExampleFitsBothChassisAndContainsHullHardware(bool heavy)
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetChassis(heavy); layout.LoadExample();
        Assert.That(layout.Heavy, Is.EqualTo(heavy));
        Assert.That(layout.Placements.Count, Is.EqualTo(7));
        Assert.That(layout.Used(false), Is.EqualTo(14));
        Assert.That(layout.Used(true), Is.EqualTo(8));
        Assert.That(layout.HasPdlWithoutBattery, Is.False);
        layout.LoadExample(); Assert.That(layout.Placements.Count, Is.EqualTo(7));
    }

    [Test]
    public void TurretRequiresDockAndHasTwelveCells()
    {
        var layout = new CustomVehicleSpaceDemo();
        Assert.That(layout.Place("weapon", true, 0, 0, false), Is.False);
        layout.SetTurret(3);
        Assert.That(layout.TurretWidth * layout.TurretHeight, Is.EqualTo(12));
        Assert.That(layout.Place("weapon", true, 0, 0, false), Is.True);
        Assert.That(layout.Place("ammo", true, 2, 0, false), Is.True);
        Assert.That(layout.Used(true), Is.EqualTo(6));
        Assert.That(layout.Place("pdl", true, 2, 3, false), Is.False);
        Assert.That(layout.Place("pdl", true, 2, 2, false), Is.True);
    }

    [TestCase("gear")]
    [TestCase("engine")]
    [TestCase("generator")]
    [TestCase("reflector")]
    public void VehicleHardwareLivesInTheHull(string module)
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetTurret(3);
        Assert.That(layout.Place(module, true, 0, 0, false), Is.False);
        Assert.That(layout.Place(module, false, 0, 0, false), Is.True);
        Assert.That(layout.Used(false), Is.EqualTo(4));
    }

    [Test]
    public void RotationCollisionAndAtomicMove()
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetTurret(3);
        Assert.That(layout.Place("ammo", true, 1, 3, false), Is.False);
        Assert.That(layout.Place("ammo", true, 1, 3, true), Is.True);
        var placed = layout.Placements.Single();
        Assert.That(layout.Place("weapon", true, 0, 2, false), Is.False);
        Assert.That(layout.Place("ammo", true, 2, 3, true, placed.Id), Is.False);
        Assert.That(layout.Placements.Single(), Is.EqualTo(placed));
        Assert.That(layout.Place("ammo", true, 0, 3, true, placed.Id), Is.True);
        Assert.That(layout.Placements.Single().Id, Is.EqualTo(placed.Id));
        Assert.That(layout.At(true, 1, 3)?.Id, Is.EqualTo(placed.Id));
        Assert.That(layout.Place("weapon", true, 0, 0, false, placed.Id), Is.False);
    }

    [Test]
    public void MultipleBatteriesAndPdlReflectorAreNotMutuallyExclusive()
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetTurret(3);
        Assert.That(layout.Place("pdl", true, 0, 0, false), Is.True);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
        Assert.That(layout.Place("battery", false, 0, 0, false), Is.True);
        Assert.That(layout.Place("battery", true, 1, 0, false), Is.True);
        Assert.That(layout.HasPdlWithoutBattery, Is.False);
        Assert.That(layout.Place("reflector", false, 1, 0, false), Is.True);
        Assert.That(layout.Placements.Count(p => p.ModuleId == "battery"), Is.EqualTo(2));
        foreach (var battery in layout.Placements.Where(p => p.ModuleId == "battery").ToArray()) layout.Remove(battery.Id);
        Assert.That(layout.HasPdlWithoutBattery, Is.True);
    }

    [Test]
    public void ChassisAndTurretChangesResetOnlyTheirOwnDemoState()
    {
        var layout = new CustomVehicleSpaceDemo(); layout.SetTurret(3);
        layout.Place("gear", false, 0, 0, false); layout.Place("weapon", true, 0, 0, false);
        layout.SetTurret(3); Assert.That(layout.Placements.Count, Is.EqualTo(2));
        layout.SetTurret(4); Assert.That(layout.Placements.Single().ModuleId, Is.EqualTo("gear"));
        layout.SetChassis(true); Assert.That(layout.Placements, Is.Empty);
        Assert.That(layout.HullWidth * layout.HullHeight, Is.EqualTo(30));
        Assert.That(layout.TurretWidth, Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.SetTurret(5));
    }

    [Test]
    public void InvalidPlacementDoesNotMutateOrOverflow()
    {
        var layout = new CustomVehicleSpaceDemo();
        Assert.That(layout.Place("battery", false, -1, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, int.MaxValue, int.MaxValue, false), Is.False);
        Assert.That(layout.Place("unknown", false, 0, 0, false), Is.False);
        Assert.That(layout.Place("battery", false, 0, 0, false, 42), Is.False);
        Assert.That(layout.Placements, Is.Empty);
    }
}
