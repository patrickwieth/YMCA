using System.IO.Compression;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class AlliedFactionTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
    static CustomFactionProfile Design(CustomFactionDesign c, string hull)
    {
        var r = c.NewRoster("england"); var p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", hull); return p;
    }
    static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry).Open()); return reader.ReadToEnd();
    }

    [TestCase("allied-medium-hull", "Challenger_Tank", "90mm", 800, 45000, 82)]
    [TestCase("designer-ranger-hull", "JEEP", "M60mg", 400, 15000, 157)]
    [TestCase("field-artillery-hull", "ARTY", "155mm", 550, 10000, 56)]
    [TestCase("designer-prism-hull", "Prismtank", "PrisTLaser", 1350, 22000, 82)]
    public void BaselinesAndFullWeaponParents(string hull, string actor, string weapon, int cost, int hp, int speed)
    {
        var c = Compiler; var p = Design(c, hull); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: " + weapon + "\n"));
        Assert.That(c.Rules(p), Does.Contain("~structures.england"));
        Assert.That(c.Rules(p), Does.Not.Contain("ProductionCostMultiplier"), "Do not erase or double-apply the stock doctrine discount.");
    }

    [Test]
    public void SharedCannonDoesNotLoseGdiHeAndDoesNotGrantItToChallenger()
    {
        var c = Compiler; var p = new CustomFactionProfile();
        c.SelectPart(p, "ammunition", "designer-he-shell");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 120mmHEAT"));
        p = Design(c, "allied-medium-hull");
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "ammunition", "designer-he-shell"));
        Assert.That(c.Weapons(p), Does.Not.Contain("120mm"));
    }

    [Test]
    public void RangerKeepsSensorsRotationAndPreDoctrineAvailability()
    {
        var c = Compiler; var p = Design(c, "designer-ranger-hull"); var v = c.Calculate(p); var rules = c.Rules(p);
        Assert.That(v.Mass, Is.EqualTo(2500)); Assert.That(v.Electric, Is.EqualTo(10)); Assert.That(v.Turn, Is.EqualTo(80));
        Assert.That(rules, Does.Contain("Turreted:\n\t\tTurnSpeed: 48"));
        Assert.That(rules, Does.Contain("weap, ~allies, ~!promotion.infantry_doctrine, ~!promotion.armored_doctrine, ~!promotion.airforce_doctrine"));
        Assert.That(c.Weapons(p), Does.Not.Contain("M60mgTD"));
    }

    [Test]
    public void PrismRetainsFullClusterAndLightArmorWheeledMovement()
    {
        var c = Compiler; var p = Design(c, "designer-prism-hull"); var rules = c.Rules(p);
        Assert.That(c.Weapons(p), Is.EqualTo("modular.custom.w0:\n\tInherits: PrisTLaser\n"));
        Assert.That(rules, Does.Contain("Locomotor: wheeled"));
        Assert.That(rules, Does.Contain("Type: Light"));
        Assert.That(rules, Does.Contain("radar, ~promotion.prism_tank"));
        Assert.That(rules, Does.Not.Contain("FirepowerMultiplier"));
        Assert.That(rules, Does.Not.Contain("Turreted:"));
    }

    [Test]
    public void AlliedArtilleryIsBoundSeparatelyFromItsNodVariant()
    {
        var c = Compiler; var p = Design(c, "field-artillery-hull");
        Assert.That(c.Rules(p), Does.Contain("Image: arty\n"));
        Assert.That(c.Rules(p), Does.Contain("Locomotor: lighttracked"));
        Assert.That(c.Rules(p), Does.Not.Contain("Turreted:"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "chassis", "designer-nod-artillery-hull"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "running_gear", "gdi-stationary"));
        p.BaseFaction = "blackh";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [Test]
    public void MapAndRoundtripUseEnglandAndCorrectNationalTankReference()
    {
        var c = Compiler; var r = c.NewRoster("england");
        Assert.That(c.AddTemplates(r), Is.Zero);
        var lab = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
        var bytes = c.CompileRosterMap(r, lab); Assert.That(c.CompileRosterMap(r, lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes)); var map = Read(zip, "map.yaml");
        foreach (var player in new[] { "Multi0", "Multi1" })
            Assert.That(map, Does.Contain("PlayerReference@" + player + ":\n\t\tName: " + player + "\n\t\tPlayable: True\n\t\tLockFaction: True\n\t\tFaction: england"));
        Assert.That(map, Does.Contain("Reference: challenger_tank"));
        Assert.That(map, Does.Not.Contain("Transport: ocar"));
        Assert.That(Read(zip, "modular-rules.yaml"), Does.Contain("FactionCA@1:"));
        var loaded = c.DeserializeRoster(Read(zip, "custom-faction.json"));
        Assert.That(loaded.BaseFaction, Is.EqualTo("england")); Assert.That(loaded.Designs.Count, Is.EqualTo(13));
        var path = Environment.GetEnvironmentVariable("MODULAR_ALLIES_EXPORT");
        if (!string.IsNullOrEmpty(path)) { using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes); }
    }
}
