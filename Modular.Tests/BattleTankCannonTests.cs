using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class BattleTankCannonTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));

    [TestCase("medium-tank-shell")]
    [TestCase("designer-he-shell")]
    public void AlternativeAddsTwentyPercentDamageWithoutChangingOtherWeaponFields(string ammo)
    {
        var c = Compiler;
        var p = new CustomFactionProfile();
        c.SelectPart(p, "ammunition", ammo);
        Assert.That(p.Parts["weapon"], Is.EqualTo("medium-cannon"));
        var before = c.Weapons(p);
        var rules = c.Rules(p);
        var cost = c.Calculate(p).Cost;
        c.SelectPart(p, "weapon", "battle-tank-120mm");
        var expected = Regex.Replace(before, @"Damage: (\d+)", m => "Damage: " +
            (int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 120 / 100).ToString(CultureInfo.InvariantCulture));
        Assert.That(c.Weapons(p), Is.EqualTo(expected));
        Assert.That(c.Weapons(p), Is.Not.EqualTo(before));
        Assert.That(c.Rules(p), Is.EqualTo(rules), "same turret, actor, prerequisites, graphics and channels");
        Assert.That(c.Calculate(p).Cost, Is.EqualTo(cost), "other provisional allocations unchanged");
        Assert.That(c.WeaponSummary(p), Does.Contain("+20% damage"));
        var restored = c.Deserialize(c.Serialize(p));
        Assert.That(restored.Parts["weapon"], Is.EqualTo("battle-tank-120mm"));
        Assert.That(c.Weapons(restored), Is.EqualTo(expected));
        c.SelectPart(p, "weapon", "medium-cannon");
        Assert.That(c.Weapons(p), Is.EqualTo(before), "105mm must remain unchanged");
    }

    [Test]
    public void SharedCarrierDoesNotAdmitTheNewWeaponToOtherHulls()
    {
        var c = Compiler;
        var p = new CustomFactionProfile { BaseFaction = "england" };
        c.SelectPart(p, "chassis", "allied-medium-hull");
        Assert.That(p.Parts["carrier"], Is.EqualTo("medium-cannon-mount"));
        Assert.That(c.CompatibleOptions(p, "weapon"), Does.Not.Contain("battle-tank-120mm"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "weapon", "battle-tank-120mm"));
        p.Parts["weapon"] = "battle-tank-120mm";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [Test]
    public void LargerWeaponConsumesInteriorSpaceWithoutChangingTurretOrSocket()
    {
        var layout = new CustomVehicleSpaceDemo { WeaponKind = CustomWeaponKind.BattleTankCannon };
        layout.LoadStockConfiguration(false);
        var original = layout.Placements.Single(p => p.ModuleId == "weapon");
        var socket = layout.WeaponSocket;
        var zones = (from y in Enumerable.Range(-1, 6) from x in Enumerable.Range(-1, 12)
                     select layout.ZoneAt(true, x, y)).ToArray();
        var used = layout.Used(true);
        layout.WeaponKind = CustomWeaponKind.BattleTank120mm;
        Assert.That(layout.WeaponSocket, Is.EqualTo(socket));
        Assert.That(layout.TurretWidth, Is.EqualTo(5));
        Assert.That(layout.TurretHeight, Is.EqualTo(3));
        Assert.That((from y in Enumerable.Range(-1, 6) from x in Enumerable.Range(-1, 12)
                     select layout.ZoneAt(true, x, y)).ToArray(), Is.EqualTo(zones));
        Assert.That(layout.ContainsCell(true, 9, 1), Is.False, "no new outside cell");
        Assert.That(layout.ModuleFor("weapon").Width, Is.EqualTo(5));
        Assert.That(layout.WeaponFootprint.X, Is.EqualTo(4));
        Assert.That(layout.CanPlace("weapon", true, 5, 1, false, original.Id), Is.False);
        Assert.That(layout.CanPlace("weapon", true, 4, 1, true, original.Id), Is.False);
        Assert.That(layout.Place("weapon", true, 4, 1, false, original.Id), Is.True);
        Assert.That(layout.At(true, 4, 1)?.Id, Is.EqualTo(original.Id));
        Assert.That(layout.At(true, 8, 1)?.Id, Is.EqualTo(original.Id));
        Assert.That(layout.Used(true), Is.EqualTo(used + 1));
        Assert.That(layout.Place("battery", true, 4, 0, false), Is.False, "breech occupies interior cell");
        layout.WeaponKind = CustomWeaponKind.BattleTankCannon;
        Assert.That(layout.Place("weapon", true, 5, 1, false, original.Id), Is.True);
        Assert.That(layout.At(true, 4, 1), Is.Null);
        Assert.That(layout.Place("battery", true, 4, 0, false), Is.True, "105mm frees the interior cell");
    }

    [TestCase("battery")]
    [TestCase("pdl")]
    [TestCase("ammo")]
    public void OccupiedBreechSpaceRejectsUpgradeWithoutRemovingAnything(string module)
    {
        var layout = new CustomVehicleSpaceDemo { WeaponKind = CustomWeaponKind.BattleTankCannon };
        layout.LoadStockConfiguration(false);
        Assert.That(layout.Place(module, true, 4, 0, false), Is.True);
        var original = layout.Placements.Single(p => p.ModuleId == "weapon");
        var before = layout.Placements.ToArray();
        layout.WeaponKind = CustomWeaponKind.BattleTank120mm;
        Assert.That(layout.Place("weapon", true, 4, 1, false, original.Id), Is.False);
        Assert.That(layout.Placements, Is.EqualTo(before));
        // Same cancellation path as the sidebar: restore kind, keep original placements.
        layout.WeaponKind = CustomWeaponKind.BattleTankCannon;
        foreach (var p in layout.Placements)
            Assert.That(layout.CanPlace(p.ModuleId, p.InTurret, p.X, p.Y, p.Rotated, p.Id), Is.True);
        layout.Remove(layout.At(true, 4, 1).Id);
        layout.WeaponKind = CustomWeaponKind.BattleTank120mm;
        Assert.That(layout.Place("weapon", true, 4, 1, false, original.Id), Is.True);
    }
}
