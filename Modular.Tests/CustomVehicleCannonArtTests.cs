using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleCannonArtTests
{
    [TestCase("gdi-battle-hull", "medium-cannon", "cannon-105mm", 1)]
    [TestCase("gdi-battle-hull", "battle-tank-120mm", "cannon-120mm", 1)]
    [TestCase("designer-titan-hull", "designer-titan-weapon", "cannon-120mm", 1)]
    [TestCase("designer-mammoth-hull", "designer-mammoth-weapons", "cannon-130mm", 2)]
    [TestCase("soviet-heavy-hull", "medium-cannon", "cannon-125mm", 2)]
    [TestCase("stock-peoples-tank-hull", "original-armament", "cannon-125mm", 2)]
    [TestCase("stock-apoc-hull", "original-armament", "cannon-130mm", 2)]
    [TestCase("stock-rtnk-hull", "original-armament", "cannon-120mm", 1)]
    [TestCase("stock-2s3-hull", "original-armament", "cannon-152mm-artillery", 1)]
    [TestCase("stock-isu-hull", "original-armament", "cannon-152mm-artillery", 1)]
    [TestCase("stock-nonasvk-hull", "original-armament", "cannon-155mm-artillery", 1)]
    [TestCase("designer-nod-artillery-hull", "field-artillery", "cannon-155mm-artillery", 1)]
    [TestCase("field-artillery-hull", "field-artillery", "cannon-155mm-artillery", 1)]
    [TestCase("nod-combat-howitzer-hull", "nod-combat-howitzer-weapon", "cannon-155mm-artillery", 1)]
    [TestCase("nod-combat-specter-hull", "nod-combat-specter-weapon", "cannon-155mm-artillery", 1)]
    public void ApprovedPicturesAreBoundToConcreteHullsAndWeapons(string hull, string weapon, string sprite, int barrels)
    {
        var art = CustomVehicleCannonArt.For(hull, weapon);
        Assert.That(art.Sprite, Is.EqualTo(sprite));
        Assert.That(art.Barrels, Is.EqualTo(barrels));
        Assert.That(art.SourceActor, Is.Not.Empty);
        Assert.That(art.SourceWeapon, Is.Not.Empty);
    }

    [Test]
    public void SharedCalculatorComponentsDoNotForceTheSameCaliberOnDifferentTanks()
    {
        Assert.That(CustomVehicleCannonArt.For("gdi-battle-hull", "medium-cannon").Label, Is.EqualTo("105mm Smoothbore Cannon"));
        Assert.That(CustomVehicleCannonArt.For("soviet-heavy-hull", "medium-cannon").Label, Is.EqualTo("Dual 125mm Cannons"));
        var challenger = CustomVehicleCannonArt.For("allied-medium-hull", "medium-cannon");
        Assert.That(challenger.Label, Is.EqualTo("90mm Cannon"));
        Assert.That(challenger.Sprite, Is.Null, "88mm/105mm are not substitutes for missing stock 90mm art");
        Assert.That(CustomVehicleCannonArt.For("allied-medium-hull", "battle-tank-120mm"), Is.Null);
        Assert.That(CustomVehicleCannonArt.For("stock-tnkd-hull", "original-armament").Sprite, Is.Null);
        Assert.That(CustomVehicleCannonArt.For("designer-mk2-hull", "designer-mk2-weapons"), Is.Null, "railguns are not ordinary shell guns");
    }

    [Test]
    public void PresentationLookupDoesNotChangeProfilesWeaponsOrAdmission()
    {
        var c = new CustomFactionDesign(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
        foreach (var faction in CustomFactionDesign.BaseFactions)
        {
            var roster = c.NewRoster(faction);
            var p = c.Profile(roster, roster.Designs[0]);
            foreach (var hull in c.CompatibleOptions(p, "chassis"))
            {
                c.SelectPart(p, "chassis", hull);
                var saved = c.Serialize(p); var rules = c.Rules(p); var weapons = c.Weapons(p);
                var options = c.CompatibleOptions(p, "weapon");
                foreach (var weapon in options) _ = CustomVehicleCannonArt.For(hull, weapon);
                Assert.That(c.Serialize(p), Is.EqualTo(saved));
                Assert.That(c.Rules(p), Is.EqualTo(rules));
                Assert.That(c.Weapons(p), Is.EqualTo(weapons));
                Assert.That(c.CompatibleOptions(p, "weapon"), Is.EqualTo(options));
            }
        }
    }
}
