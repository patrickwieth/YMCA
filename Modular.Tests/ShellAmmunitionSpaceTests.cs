using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class ShellAmmunitionSpaceTests
{
    [TestCase("medium-tank-shell", CustomWeaponKind.BattleTankCannon)]
    [TestCase("designer-he-shell", CustomWeaponKind.BattleTankCannon)]
    [TestCase("medium-tank-shell", CustomWeaponKind.BattleTank120mm)]
    [TestCase("designer-he-shell", CustomWeaponKind.BattleTank120mm)]
    public void ShellsOccupyTwoByTwoInStockAndHeldFootprints(string ammo, CustomWeaponKind weapon)
    {
        var layout = new CustomVehicleSpaceDemo { AmmunitionId = ammo, WeaponKind = weapon };
        layout.LoadStockConfiguration(false);
        Assert.That(layout.Placements.Count, Is.EqualTo(6));
        Assert.That(CustomVehicleSpaceDemo.Size(layout.ModuleFor("ammo"), false), Is.EqualTo((2, 2)));
        Assert.That(CustomVehicleSpaceDemo.Size(layout.ModuleFor("ammo"), true), Is.EqualTo((2, 2)));
        var block = layout.Placements.Single(p => p.ModuleId == "ammo");
        Assert.That(layout.At(true, 1, 1)?.Id, Is.EqualTo(block.Id));
        Assert.That(layout.Place("battery", true, 1, 0, false), Is.False);
        Assert.That(layout.Place("ammo", true, 3, 2, true, block.Id), Is.False);
        Assert.That(layout.Place("ammo", true, 3, 0, false, block.Id),
            Is.EqualTo(weapon == CustomWeaponKind.BattleTankCannon), "120mm breech blocks the right-hand square");
        layout.LoadExample();
        Assert.That(layout.Placements.Count, Is.EqualTo(8));
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True);
    }

    [TestCase("scout-mg-rounds")]
    [TestCase("designer-rocket-payload")]
    [TestCase("integral-stores")]
    public void OtherAmmunitionKeepsItsExistingSize(string ammo)
    {
        var layout = new CustomVehicleSpaceDemo { AmmunitionId = ammo };
        Assert.That(CustomVehicleSpaceDemo.Size(layout.ModuleFor("ammo"), false), Is.EqualTo((1, 2)));
    }

    [Test]
    public void ExpandingHeldAmmunitionCannotHideAnOccupiedCellBehindItsMovingBlock()
    {
        var layout = new CustomVehicleSpaceDemo { WeaponKind = CustomWeaponKind.BattleTankCannon };
        layout.SetChassis(false); layout.SetTurret(5);
        layout.Place("ammo", true, 0, 0, false);
        var ammo = layout.Placements.Single();
        layout.Place("pdl", true, 1, 0, false);
        var before = layout.Placements.ToArray();
        layout.AmmunitionId = "designer-he-shell";
        Assert.That(layout.Place("ammo", true, 0, 0, false, ammo.Id), Is.False);
        Assert.That(layout.Placements, Is.EqualTo(before));
        layout.AmmunitionId = null; // cancelled pickup
        Assert.That(layout.CanPlace("ammo", true, 0, 0, false, ammo.Id), Is.True);
    }
}
