using System.IO.Compression;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class NodFactionTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));
    static CustomFactionDesign Compiler => new(Catalog);
    static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry).Open()); return reader.ReadToEnd();
    }

    [TestCase("nod-light-hull", 625, 41250, 100, "LTNK", "30mm")]
    [TestCase("buggy-hull", 350, 14000, 157, "BGGY", "M60mgTD")]
    [TestCase("designer-nod-artillery-hull", 550, 10000, 56, "ARTY.nod", "155mmTD")]
    [TestCase("designer-ssm-hull", 1050, 15000, 82, "SSM", "HonestJohn")]
    [TestCase("nod-combat-apc-hull", 600, 30000, 135, "APC2", "M60mgTD")]
    [TestCase("nod-combat-bike-hull", 500, 11000, 180, "BIKE", "BikeRockets")]
    [TestCase("nod-combat-beam-hull", 1250, 24000, 100, "Beam_Cannon", "BeamCannon")]
    [TestCase("nod-combat-flame-hull", 700, 40000, 82, "FTNK", "BigFlamer")]
    [TestCase("nod-combat-heavy-flame-hull", 1000, 75000, 68, "HFTK", "HeavyFlameTankFlamer")]
    [TestCase("nod-combat-howitzer-hull", 550, 15000, 68, "HOWI", "155mmTDM")]
    [TestCase("nod-combat-specter-hull", 1100, 11000, 100, "SPEC", "155mmSpec")]
    [TestCase("nod-combat-stealth-hull", 1200, 20000, 135, "STNK", "StnkMissile")]
    [TestCase("nod-combat-chemical-hull", 1200, 10000, 92, "TTRK", "DemoTruckTargeting")]
    [TestCase("nod-combat-microwave-hull", 1250, 35000, 100, "WTNK", "MicrowaveZap")]
    public void BaselinesAndCompleteWeaponBindings(string hull, int cost, int hp, int speed, string actor, string weapon)
    {
        var c = Compiler; var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", hull); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: " + weapon + "\n"));
        Assert.That(c.Rules(p), Does.Contain("FactionCA@13"));
        Assert.That(c.Rules(p), Does.Contain("~structures.blackh"));
        Assert.That(c.Rules(p), Does.Not.Contain("structures.eagle"));
        Assert.That(c.CompatibleOptions(p, "running_gear"), Does.Not.Contain("gdi-stationary"));
    }

    [Test]
    public void FamilyFilteringAndCrossFactionRejectionAreNativeNotJustUi()
    {
        var c = Compiler; var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        Assert.That(c.CompatibleOptions(p, "chassis").Length, Is.EqualTo(14));
        Assert.That(c.CompatibleOptions(new CustomFactionProfile(), "chassis").Length, Is.EqualTo(11));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "chassis", "gdi-battle-hull"));
        p.BaseFaction = "eagle";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
        p = c.Profile(r, r.Designs[0]); p.Parts["running_gear"] = "gdi-stationary";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
        Assert.Throws<InvalidDataException>(() => c.NewRoster("legion"));
    }

    [Test]
    public void CatalogCannotReassignAnAssemblyToAnotherFaction()
    {
        var data = JObject.Parse(Catalog); data["assemblies"][0]["faction"] = "nod";
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()));
    }

    [Test]
    public void NewCopySaveAndLoadKeepNodAndIndependentParts()
    {
        var c = Compiler; var r = c.NewRoster("blackh");
        var added = c.AddDesign(r); var copy = c.AddDesign(r, added);
        c.SelectPart(c.Profile(r, copy), "chassis", "buggy-hull");
        Assert.That(added.Parts["chassis"], Is.EqualTo("nod-light-hull"));
        var loaded = c.DeserializeRoster(c.SerializeRoster(r));
        Assert.That(loaded.BaseFaction, Is.EqualTo("blackh"));
        Assert.That(loaded.Designs.Count, Is.EqualTo(3));
        Assert.That(c.SerializeRoster(loaded), Is.EqualTo(c.SerializeRoster(r)));
        var old = new CustomFactionRoster(); var frozen = c.SerializeRoster(old);
        c.NewRoster("blackh");
        Assert.That(c.SerializeRoster(old), Is.EqualTo(frozen));
    }

    [Test]
    public void SwitchingBaseReservesANewLibraryNameInsteadOfOverwritingAnExistingFaction()
    {
        var c = Compiler; var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var old = new CustomFactionRoster { Name = "Meine NOD" }; c.SaveLibrary(dir, old);
            var next = c.NewRoster("blackh", dir);
            Assert.That(next.Name, Is.EqualTo("Meine NOD 2")); c.SaveLibrary(dir, next);
            Assert.That(c.NewRoster("blackh", dir).Name, Is.EqualTo("Meine NOD 3"));
            Assert.That(Directory.GetFiles(dir, "*.json").Length, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(dir, "*.json").Select(f => c.DeserializeRoster(File.ReadAllText(f)).BaseFaction),
                Is.EquivalentTo(new[] { "eagle", "blackh" }));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Test]
    public void SsmRetainsAmmoReloadAndUnlockByInheritanceWithoutGenericOverrides()
    {
        var c = Compiler; var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", "designer-ssm-hull"); var rules = c.Rules(p);
        Assert.That(rules, Does.Contain("tmpl, ~promotion.ssm_launcher"));
        Assert.That(rules, Does.Not.Contain("AmmoPool:"));
        Assert.That(rules, Does.Not.Contain("Turreted:"));
        Assert.That(rules, Does.Not.Contain("FireDelay:"));
        Assert.That(c.Weapons(p), Is.EqualTo("modular.custom.w0:\n\tInherits: HonestJohn\n"));
    }

    [Test]
    public void BuggyCountsSensorsOnceAndKeepsPromotionExclusions()
    {
        var c = Compiler; var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", "buggy-hull"); var v = c.Calculate(p);
        Assert.That(v.Mass, Is.EqualTo(2200)); Assert.That(v.Electric, Is.EqualTo(10));
        Assert.That(c.Rules(p), Does.Contain("~!promotion.buggy.pdl, ~!promotion.buggy.aa"));
    }

    [Test]
    public void MapLocksBothPlayersToNodAndFreezesAllFourteenFamilies()
    {
        var c = Compiler; var r = c.NewRoster("blackh");
        foreach (var hull in c.CompatibleOptions(c.Profile(r, r.Designs[0]), "chassis").Skip(1))
        {
            var d = c.AddDesign(r); c.SelectPart(c.Profile(r, d), "chassis", hull);
        }
        var bytes = c.CompileRosterMap(r, File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap")));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var map = Read(zip, "map.yaml");
        foreach (var player in new[] { "Multi0", "Multi1" })
            Assert.That(map, Does.Contain("PlayerReference@" + player + ":\n\t\tName: " + player + "\n\t\tPlayable: True\n\t\tLockFaction: True\n\t\tFaction: blackh"));
        Assert.That(map, Does.Contain("Reference: ltnk"));
        Assert.That(map, Does.Not.Contain("Transport: ocar"));
        Assert.That(Read(zip, "modular-rules.yaml"), Does.Contain("FactionCA@13"));
        var loaded = c.DeserializeRoster(Read(zip, "custom-faction.json"));
        Assert.That(loaded.Designs.Count, Is.EqualTo(14)); Assert.That(loaded.BaseFaction, Is.EqualTo("blackh"));
        var path = Environment.GetEnvironmentVariable("MODULAR_NOD_EXPORT");
        if (!string.IsNullOrEmpty(path)) { using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes); }
    }
}
