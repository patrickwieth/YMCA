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
    public void LargerWeaponAddsOneExternalCellAndCanReturnToTheOldFootprint()
    {
        var layout = new CustomVehicleSpaceDemo { WeaponKind = CustomWeaponSocket.ForParts("medium-cannon-mount", "medium-cannon") };
        layout.LoadStockConfiguration(false);
        var original = layout.Placements.Single(p => p.ModuleId == "weapon");
        Assert.That(layout.ContainsCell(true, 9, 1), Is.False);
        layout.WeaponKind = CustomWeaponSocket.ForParts("medium-cannon-mount", "battle-tank-120mm");
        Assert.That(layout.WeaponSocket.X, Is.EqualTo(5));
        Assert.That(layout.ModuleFor("weapon").Width, Is.EqualTo(5));
        Assert.That(layout.ContainsCell(true, 9, 1), Is.True);
        Assert.That(layout.At(true, 9, 1)?.Id, Is.EqualTo(original.Id));
        Assert.That(layout.Place("battery", true, 9, 1, false), Is.False);
        Assert.That(layout.CanPlace("weapon", true, 5, 1, false, original.Id), Is.True);
        Assert.That(layout.CanPlace("weapon", true, 5, 1, true, original.Id), Is.False);
        layout.WeaponKind = CustomWeaponKind.BattleTankCannon;
        Assert.That(layout.ContainsCell(true, 9, 1), Is.False);
        Assert.That(layout.ModuleFor("weapon").Width, Is.EqualTo(4));
        Assert.That(layout.Placements.Single(p => p.ModuleId == "weapon"), Is.EqualTo(original));
    }
}
