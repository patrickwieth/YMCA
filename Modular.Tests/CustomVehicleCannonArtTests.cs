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
    [TestCase("stock-nonasvk-hull", "original-armament", "cannon-152mm-artillery", 1)]
    [TestCase("designer-nod-artillery-hull", "field-artillery", "cannon-155mm-artillery", 1)]
    [TestCase("field-artillery-hull", "field-artillery", "cannon-155mm-artillery", 1)]
    [TestCase("nod-combat-howitzer-hull", "nod-combat-howitzer-weapon", "cannon-155mm-artillery", 1)]
    [TestCase("nod-combat-specter-hull", "nod-combat-specter-weapon", "cannon-155mm-artillery", 1)]
    [TestCase("allied-medium-hull", "medium-cannon", "cannon-88mm", 1)]
    [TestCase("stock-1tnk-hull", "original-armament", "cannon-75mm", 1)]
    [TestCase("nod-light-hull", "light-cannon", "cannon-75mm", 1)]
    [TestCase("t34-hull", "light-cannon", "cannon-75mm", 1)]
    [TestCase("stock-devil-tank-hull", "original-armament", "cannon-75mm", 1)]
    [TestCase("stock-tnkd-hull", "original-armament", "cannon-155mm-artillery", 1)]
    [TestCase("stock-tsar-tank-hull", "original-armament", "cannon-152mm-short", 1)]
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
        Assert.That(challenger.Label, Is.EqualTo("88mm Cannon"));
        Assert.That(challenger.Sprite, Is.EqualTo("cannon-88mm"));
        Assert.That(challenger.SourceWeapon, Is.EqualTo("90mm"), "Presentation reclassification preserves the native rule ID");
        Assert.That(CustomVehicleCannonArt.For("allied-medium-hull", "battle-tank-120mm"), Is.Null);
        Assert.That(CustomVehicleCannonArt.For("stock-tnkd-hull", "original-armament").Sprite, Is.EqualTo("cannon-155mm-artillery"));
        Assert.That(CustomVehicleCannonArt.For("designer-mk2-hull", "designer-mk2-weapons"), Is.Null, "railguns are not ordinary shell guns");
    }

    [TestCase("nod-light-hull", "light-cannon")]
    [TestCase("t34-hull", "light-cannon")]
    [TestCase("stock-devil-tank-hull", "original-armament")]
    public void LegacyCannonsAreNotTheFutureRapidFireAutocannon(string hull, string weapon)
    {
        var art = CustomVehicleCannonArt.For(hull, weapon);
        Assert.That(art.Label, Is.EqualTo("75mm Cannon"));
        Assert.That(art.SourceWeapon, Is.EqualTo("30mm"));
        Assert.That(CustomVehicleCannonArt.For(hull, "30mm-autocannon"), Is.Null);
    }

    [Test]
    public void ArtilleryCalibersFollowFactionDirectionWithoutChangingNativeRules()
    {
        foreach (var hull in new[] { "stock-2s3-hull", "stock-isu-hull", "stock-nonasvk-hull", "stock-tsar-tank-hull" })
            Assert.That(CustomVehicleCannonArt.For(hull, "original-armament").Label, Does.StartWith("152mm"));
        Assert.That(CustomVehicleCannonArt.For("stock-nonasvk-hull", "original-armament").SourceWeapon, Is.EqualTo("155mm.Nona"));
        Assert.That(CustomVehicleCannonArt.For("field-artillery-hull", "field-artillery").Label, Does.StartWith("155mm"));
        Assert.That(CustomVehicleCannonArt.For("stock-tnkd-hull", "original-armament").Label, Does.StartWith("155mm"));
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
