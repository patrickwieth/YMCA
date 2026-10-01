using System.IO.Compression;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class ScrinFactionTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
    static CustomFactionProfile Design(CustomFactionDesign c, string key)
    {
        var r = c.NewRoster("traveler"); var p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", "scrin-" + key + "-hull"); return p;
    }
    static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry).Open()); return reader.ReadToEnd();
    }

    [TestCase("gunwalker", "GUNW", 650, 30000, 113, 8, "wheeled")]
    [TestCase("seeker", "SEEK", 800, 20000, 135, 48, "lighthover")]
    [TestCase("corrupter", "CORR", 700, 45000, 82, 24, "wheeled")]
    [TestCase("devourer", "DEVO", 1250, 35000, 90, 1000, "lighthover")]
    public void RawBaselinesAndActualMovementProfiles(string key, string actor, int cost, int hp, int speed, int turn, string locomotor)
    {
        var c = Compiler; var p = Design(c, key); var v = c.Calculate(p); var rules = c.Rules(p);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(v.Turn, Is.EqualTo(turn)); Assert.That(rules, Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(rules, Does.Contain("Locomotor: " + locomotor)); Assert.That(rules, Does.Contain("Type: Light"));
        Assert.That(rules, Does.Contain("~structures.traveler"));
        Assert.That(rules, Does.Not.Contain("\tCarryable:")); Assert.That(rules, Does.Not.Contain("\tTurreted:"));
        Assert.That(c.Weapons(p), Does.Not.Contain("Warhead@"));
    }

    [Test]
    public void NativeHoverIsNotAddedTwiceAndDevourerTurretIsNotConverted()
    {
        var c = Compiler;
        foreach (var key in new[] { "seeker", "devourer" })
        {
            var p = Design(c, key); Assert.That(c.IsNativeHover(p), Is.True);
            Assert.That(c.Rules(p), Does.Not.Contain("Inherits@MODULARHOVER"));
            Assert.That(c.Rules(p), Does.Not.Contain("Hovers:"));
            Assert.That(c.Rules(p), Does.Not.Contain("KillsSelf@SINK:"));
        }
        var devo = Design(c, "devourer");
        Assert.That(c.Rules(devo), Does.Not.Contain("TurretedFloating:"), "Inherit complete floating turret without numeric rewrite.");
        Assert.That(c.Weapons(devo), Is.EqualTo("modular.custom.w0:\n\tInherits: DevourerLaser\n"));
        Assert.That(c.Rules(devo), Does.Contain("radar, ~traveler, ~!upg.devourer"));
    }

    [Test]
    public void GunwalkerAndCorrupterKeepFullWeaponsAndAnimationParents()
    {
        var c = Compiler; var gun = Design(c, "gunwalker");
        Assert.That(c.Weapons(gun), Is.EqualTo("modular.custom.w0:\n\tInherits: GunWalkerZap\nmodular.custom.w1:\n\tInherits: GunWalkerZapAA\n"));
        Assert.That(c.Rules(gun), Does.Contain("wsph, ~scrin, ~!upg.gunwalker"));
        var corr = Design(c, "corrupter");
        Assert.That(c.Weapons(corr), Does.Contain("Inherits: CorrupterSpew\n"));
        foreach (var key in new[] { "WithMoveAnimation:", "WithAttackAnimation:", "AttackFrontal:", "FireWarheadsOnDeathCA:" })
            Assert.That(c.Rules(corr), Does.Not.Contain(key));
    }

    [Test]
    public void DriveAndConverterChoicesAreBoundAndCannotLeakToOtherFactions()
    {
        var c = Compiler; var p = Design(c, "seeker");
        Assert.That(c.CompatibleOptions(p, "generator"), Is.EquivalentTo(new[] { "scrin-converter", "scrin-converter-efficient" }));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "drive", "diesel"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "generator", "efficient-generator"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "running_gear", "gdi-stationary"));
        var before = c.Calculate(p); c.SelectPart(p, "generator", "scrin-converter-efficient");
        Assert.That(c.Calculate(p).Cost - before.Cost, Is.EqualTo(200));
        Assert.That(c.Calculate(p).Reserve, Is.GreaterThan(before.Reserve));
        var gdi = new CustomFactionProfile(); gdi.Parts["generator"] = "scrin-converter";
        Assert.Throws<InvalidDataException>(() => c.Calculate(gdi));
    }

    [Test]
    public void SixBasesExistAndScrinMapFreezesFourFamiliesWithTravelerReference()
    {
        var c = Compiler; Assert.That(CustomFactionDesign.BaseFactions.Length, Is.EqualTo(6));
        var r = c.NewRoster("traveler");
        foreach (var hull in c.CompatibleOptions(c.Profile(r, r.Designs[0]), "chassis").Skip(1))
        {
            var d = c.AddDesign(r); c.SelectPart(c.Profile(r, d), "chassis", hull);
            d.Name = "Eigen " + c.Label(hull).Split(new[] { " - " }, StringSplitOptions.None)[0];
        }
        var lab = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
        var bytes = c.CompileRosterMap(r, lab); Assert.That(c.CompileRosterMap(r, lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes)); var map = Read(zip, "map.yaml");
        foreach (var player in new[] { "Multi0", "Multi1" })
            Assert.That(map, Does.Contain("PlayerReference@" + player + ":\n\t\tName: " + player + "\n\t\tPlayable: True\n\t\tLockFaction: True\n\t\tFaction: traveler"));
        Assert.That(map, Does.Contain("Reference: seek")); Assert.That(map, Does.Not.Contain("Transport: ocar"));
        Assert.That(Read(zip, "modular-rules.yaml"), Does.Contain("FactionCA@18:"));
        var loaded = c.DeserializeRoster(Read(zip, "custom-faction.json"));
        Assert.That(loaded.BaseFaction, Is.EqualTo("traveler")); Assert.That(loaded.Designs.Count, Is.EqualTo(4));
        var path = Environment.GetEnvironmentVariable("MODULAR_SCRIN_EXPORT");
        if (!string.IsNullOrEmpty(path)) { using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes); }
    }
}
