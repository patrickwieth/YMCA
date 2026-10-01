using System.IO.Compression;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class SovietFactionTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
    static CustomFactionProfile Design(CustomFactionDesign c, string hull)
    {
        var r = c.NewRoster("russia"); var p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", hull); return p;
    }
    static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry).Open()); return reader.ReadToEnd();
    }

    [TestCase("soviet-heavy-hull", "Heavy_Tank", 1100, 65000, 68)]
    [TestCase("t34-hull", "T-34", 600, 42000, 100)]
    [TestCase("heavy-tesla", "TTNK.RA2", 1350, 48000, 100)]
    [TestCase("designer-flak-hull", "FTRK", 500, 15000, 118)]
    public void RawBaselinesAndNativeAdmission(string hull, string actor, int price, int hp, int speed)
    {
        var c = Compiler; var p = Design(c, hull); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(price)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.Rules(p), Does.Contain("~structures.russia"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "running_gear", "gdi-stationary"));
    }

    [Test]
    public void T34KeepsBothClusterStatesWithoutChangingNodLightTank()
    {
        var c = Compiler; var p = Design(c, "t34-hull");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 30mm\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 30mm.Cluster_Upgrade\n"));
        Assert.That(c.Rules(p), Does.Contain("Armament@primary-cluster:\n\t\tWeapon: modular.custom.w1"));
        Assert.That(c.Rules(p), Does.Not.Contain("RequiresCondition:"));
        Assert.That(c.Rules(p), Does.Not.Contain("vehicles.nkorea"), "Explicit custom Soviet family admission, not a vanilla roster change.");
        var nod = c.NewRoster("blackh");
        Assert.That(c.Weapons(c.Profile(nod, nod.Designs[0])), Does.Not.Contain("Cluster_Upgrade"));
    }

    [Test]
    public void HeavyTankUsesTwinCadenceWithoutChangingSharedGdiGun()
    {
        var c = Compiler; var p = Design(c, "soviet-heavy-hull"); var weapon = c.Weapons(p);
        Assert.That(weapon, Does.Contain("Inherits: 125mm\n"));
        Assert.That(weapon, Does.Contain("Burst: 2")); Assert.That(weapon, Does.Contain("ReloadDelay: 70"));
        Assert.That(c.Rules(p), Does.Contain("~vehicles.russia, radar, ~!upg.heavy_tank"));
        var gdi = new CustomFactionProfile();
        Assert.That(c.Weapons(gdi), Does.Contain("Burst: 1")); Assert.That(c.Weapons(gdi), Does.Contain("ReloadDelay: 50"));
    }

    [Test]
    public void TeslaKeepsAttackAnimationDelayAndFullDischarge()
    {
        var c = Compiler; var p = Design(c, "heavy-tesla"); var rules = c.Rules(p);
        Assert.That(c.Weapons(p), Is.EqualTo("modular.custom.w0:\n\tInherits: TTankZapMK2\n"));
        foreach (var field in new[] { "FireDelay:", "Turreted:", "WithTurretAttackAnimation:", "FirepowerMultiplier" })
            Assert.That(rules, Does.Not.Contain(field), "Do not overwrite complete stock attack/upgrade traits.");
        Assert.That(rules, Does.Contain("dome, ~vehicles.russia, ~!promotion.tesla_arc"));
        Assert.That(c.Calculate(p).Electric, Is.EqualTo(60), "Tesla demand is counted once.");
    }

    [Test]
    public void FlakRetainsIndependentGroundAndAirPackages()
    {
        var c = Compiler; var p = Design(c, "designer-flak-hull"); var rules = c.Rules(p);
        Assert.That(c.Weapons(p), Is.EqualTo("modular.custom.w0:\n\tInherits: FLAK-23-AA\nmodular.custom.w1:\n\tInherits: FLAK-23-AG\n"));
        Assert.That(rules, Does.Contain("Armament@AA:\n\t\tWeapon: modular.custom.w0"));
        Assert.That(rules, Does.Contain("Armament@AG:\n\t\tWeapon: modular.custom.w1"));
        Assert.That(rules, Does.Contain("Locomotor: wheeled")); Assert.That(rules, Does.Contain("Type: Light"));
        Assert.That(rules, Does.Contain("~!promotion.flak_track.barrage"));
    }

    [Test]
    public void FrozenRussiaRosterHasFourFamiliesAndMatchingReference()
    {
        var c = Compiler; var r = c.NewRoster("russia");
        foreach (var hull in c.CompatibleOptions(c.Profile(r, r.Designs[0]), "chassis").Skip(1))
        {
            var d = c.AddDesign(r); c.SelectPart(c.Profile(r, d), "chassis", hull);
            d.Name = "Eigen " + c.Label(hull).Split(new[] { " - " }, StringSplitOptions.None)[0];
        }
        var lab = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
        var bytes = c.CompileRosterMap(r, lab); Assert.That(c.CompileRosterMap(r, lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes)); var map = Read(zip, "map.yaml");
        foreach (var player in new[] { "Multi0", "Multi1" })
            Assert.That(map, Does.Contain("PlayerReference@" + player + ":\n\t\tName: " + player + "\n\t\tPlayable: True\n\t\tLockFaction: True\n\t\tFaction: russia"));
        Assert.That(map, Does.Contain("Reference: heavy_tank")); Assert.That(map, Does.Not.Contain("Transport: ocar"));
        Assert.That(Read(zip, "modular-rules.yaml"), Does.Contain("FactionCA@5:"));
        var loaded = c.DeserializeRoster(Read(zip, "custom-faction.json"));
        Assert.That(loaded.Designs.Count, Is.EqualTo(4)); Assert.That(loaded.BaseFaction, Is.EqualTo("russia"));
        var p = c.Profile(loaded, loaded.Designs[0]); p.BaseFaction = "england";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
        var path = Environment.GetEnvironmentVariable("MODULAR_SOVIET_EXPORT");
        if (!string.IsNullOrEmpty(path)) { using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes); }
    }
}
