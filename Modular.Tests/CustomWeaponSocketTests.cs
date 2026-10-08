using NUnit.Framework;
using OpenRA.Mods.CA.Modular;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace Modular.Tests;

public class CustomWeaponSocketTests
{
    static CustomVehicleSpaceDemo Layout(CustomWeaponKind kind)
    {
        var layout = new CustomVehicleSpaceDemo { WeaponKind = kind };
        layout.SetChassis(false); layout.SetTurret(5); return layout;
    }

    [Test]
    public void CannonSocketProtrudesWithoutAddingAnEntireExtraStorageColumn()
    {
        var layout = Layout(CustomWeaponKind.Cannon);
        Assert.That(layout.WeaponSocket, Is.EqualTo(new CustomWeaponSocket(CustomWeaponKind.Cannon, 4, 1, 4, 1)));
        for (var x = 4; x < 8; x++)
        {
            Assert.That(layout.ContainsCell(true, x, 1), Is.True);
            Assert.That(layout.ZoneAt(true, x, 1), Is.EqualTo(Zone.Weapon));
        }
        Assert.That(layout.ContainsCell(true, 5, 0), Is.False);
        Assert.That(layout.ContainsCell(true, 5, 2), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 1, false), Is.True);
        Assert.That(layout.At(true, 7, 1)?.ModuleId, Is.EqualTo("weapon"));
        Assert.That(layout.Used(true), Is.EqualTo(4));
        var original = layout.Placements.Single();
        Assert.That(layout.Place("weapon", true, 3, 1, false, original.Id), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 0, false, original.Id), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 1, true, original.Id), Is.False);
        Assert.That(layout.Placements.Single(), Is.EqualTo(original));
    }

    [Test]
    public void OnlyWeaponCanUseProtrudingCells()
    {
        var layout = Layout(CustomWeaponKind.Cannon);
        foreach (var id in new[] { "ammo", "battery", "pdl", "armor", "engine", "generator" })
            Assert.That(layout.Place(id, true, 5, 1, true), Is.False, id);
        Assert.That(layout.Place("weapon", false, 4, 1, false), Is.False);
        Assert.That(layout.Place("weapon", true, int.MaxValue, 1, false), Is.False);
        Assert.That(layout.ContainsCell(true, int.MaxValue, int.MaxValue), Is.False);
    }

    [Test]
    public void SonicSocketIsFiveByOneInsideTheTopRow()
    {
        var layout = Layout(CustomWeaponKind.SonicEmitter);
        Assert.That(layout.WeaponSocket, Is.EqualTo(new CustomWeaponSocket(CustomWeaponKind.SonicEmitter, 0, 0, 5, 1)));
        Assert.That(layout.ModuleFor("weapon").Width, Is.EqualTo(5));
        Assert.That(layout.ModuleFor("weapon").Height, Is.EqualTo(1));
        for (var x = 0; x < 5; x++)
        {
            Assert.That(layout.ZoneAt(true, x, 0), Is.EqualTo(Zone.Weapon));
            Assert.That(layout.ZoneAt(true, x, 1), Is.EqualTo(Zone.Ammunition));
        }
        Assert.That(layout.ContainsCell(true, 5, 0), Is.False);
        Assert.That(layout.Place("ammo", true, 0, 0, true), Is.False);
        Assert.That(layout.Place("weapon", true, 0, 0, false), Is.True);
        Assert.That(layout.Place("ammo", true, 0, 1, false), Is.True);
        var weapon = layout.Placements.First();
        Assert.That(layout.Place("weapon", true, 0, 1, false, weapon.Id), Is.False);
        Assert.That(layout.Place("weapon", true, 0, 0, true, weapon.Id), Is.False);
        layout.Remove(weapon.Id);
        Assert.That(layout.Place("battery", true, 0, 0, true), Is.True, "free modules still obey their inside-container color exception");
        Assert.That(layout.Place("weapon", true, 0, 0, false), Is.False, "occupied socket");
    }

    [TestCase("sonic-turret", CustomWeaponKind.SonicEmitter)]
    [TestCase("dual-gatling-turret", CustomWeaponKind.DualGatling)]
    [TestCase("designer-marv-mount", CustomWeaponKind.TripleIon)]
    [TestCase("designer-hmlrs-mount", CustomWeaponKind.MissileLauncher)]
    [TestCase("prism-turret", CustomWeaponKind.PrismEmitter)]
    public void CarrierSelectsItsBoundSchematic(string carrier, CustomWeaponKind expected)
        => Assert.That(CustomWeaponSocket.ForCarrier(carrier), Is.EqualTo(expected));

    [Test]
    public void AllWeaponKindsRestoreValidStockAndPlanningBlocks()
    {
        foreach (var kind in Enum.GetValues<CustomWeaponKind>())
        {
            var layout = Layout(kind);
            layout.LoadStockConfiguration(false);
            Assert.That(layout.Placements.Count, Is.EqualTo(6), kind.ToString());
            layout.LoadExample();
            Assert.That(layout.Placements.Count, Is.EqualTo(8), kind.ToString());
            foreach (var p in layout.Placements)
                Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True, kind + "/" + p.ModuleId);
        }
    }
}
