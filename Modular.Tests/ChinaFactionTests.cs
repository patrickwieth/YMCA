using System.IO.Compression;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class ChinaFactionTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
    static CustomFactionProfile Design(CustomFactionDesign c, string hull)
    {
        var r = c.NewRoster("chinatnk"); var p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", hull); return p;
    }
    static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry).Open()); return reader.ReadToEnd();
    }

    [TestCase("battlemaster", "chbattle", 950, 40000, 100, 48)]
    [TestCase("dragon-chassis", "chdragon", 600, 28000, 103, 48)]
    [TestCase("gatling-chassis", "chgtnk", 800, 30000, 108, 48)]
    [TestCase("overlord-chassis", "choverlord", 2000, 95000, 56, 8)]
    public void RawBaselinesAndBinding(string hull, string actor, int cost, int hp, int speed, int turn)
    {
        var c = Compiler; var p = Design(c, hull); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp));
        Assert.That(v.Speed, Is.EqualTo(speed)); Assert.That(v.Turn, Is.EqualTo(turn));
        var rules = c.Rules(p);
        Assert.That(rules, Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(rules, Does.Contain("~structures.chinatnk"));
        Assert.That(rules, Does.Contain("~vehicles.china"));
        Assert.That(rules, Does.Not.Contain("\tTurreted:"), "Keep original turret geometry and rotation speed.");
        Assert.That(rules, Does.Not.Contain("\tCarryable:"));
        Assert.That(c.Weapons(p), Does.Not.Contain("Warhead@"));
    }

    [TestCase("inferno", "charty", 900, 12000, 90, 48)]
    [TestCase("crawler", "chcrawl2", 1000, 45000, 125, 48)]
    [TestCase("nuke", "chnukecann", 2400, 24000, 55, 8)]
    [TestCase("bixi", "Bixi", 900, 16000, 56, 8)]
    public void FurtherCombatBaselines(string key, string actor, int cost, int hp, int speed, int turn)
    {
        var c = Compiler; var p = Design(c, "china-combat-" + key + "-hull"); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp));
        Assert.That(v.Speed, Is.EqualTo(speed)); Assert.That(v.Turn, Is.EqualTo(turn));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.Rules(p), Does.Contain("~structures.chinatnk"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tCargo:"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tMissileSpawnerMasterCA:"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tGrantConditionOnDeploy:"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tAmmoPool:"));
        Assert.That(c.Weapons(p), Does.Not.Contain("ReloadDelay:"));
    }

    [Test]
    public void PassengerWeaponsAndSpawnedMissilesAreNotReplacedByGenericCannons()
    {
        var c = Compiler; var p = Design(c, "china-combat-crawler-hull");
        Assert.That(c.Weapons(p), Is.Empty);
        Assert.That(c.Rules(p), Does.Not.Contain("\tArmament"));
        Assert.That(c.Rules(p), Does.Contain("chweap, ~!upg.crawler, ~structures.chinatnk"));
        c.SelectPart(p, "chassis", "china-combat-nuke-hull");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: CHNukeCannon\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: NukeCannonDummyAim\n"));
        Assert.That(c.Rules(p), Does.Contain("~nuke_cannon.access, ~!upg.nuke_cannon"));
        c.SelectPart(p, "chassis", "china-combat-bixi-hull");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: BixiLauncher\n"));
        Assert.That(c.Rules(p), Does.Contain("~promotion.bixi_dragon"));
        c.SelectPart(p, "chassis", "china-combat-inferno-hull");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: CHInfernoCannon.Black_Napalm\n"));
    }

    [Test]
    public void DragonKeepsEveryDeployAndNapalmChannelWithoutOverridingStateTraits()
    {
        var c = Compiler; var p = Design(c, "dragon-chassis"); var rules = c.Rules(p); var weapons = c.Weapons(p);
        var channels = new[] { "CHDragonFlamer", "CHDragonFlamer.Black_Napalm", "CHDragonFirestorm", "CHDragonFirestorm2" };
        for (var i = 0; i < channels.Length; i++)
            Assert.That(weapons, Does.Contain("modular.custom.w" + i + ":\n\tInherits: " + channels[i] + "\n"));
        Assert.That(rules, Does.Contain("Armament@Firewall1:\n\t\tWeapon: modular.custom.w2"));
        Assert.That(rules, Does.Contain("Armament@Firewall2:\n\t\tWeapon: modular.custom.w3"));
        foreach (var field in new[] { "RequiresCondition:", "RequireForceMoveCondition:", "GrantConditionOnDeploy:", "GrantConditionOnAttack@Firewall:" })
            Assert.That(rules, Does.Not.Contain(field), "Must inherit original deploy/cycle/Black Napalm state.");
        Assert.That(rules, Does.Contain("~!promotion.dragon_tank.pdl, ~!promotion.dragon_tank.reflector"));
    }

    [Test]
    public void GatlingHasEightUniqueChannelsAndDoesNotFlattenSpinup()
    {
        var c = Compiler; var p = Design(c, "gatling-chassis"); var weapons = c.Weapons(p); var rules = c.Rules(p);
        for (var i = 0; i < 8; i++)
        {
            var suffix = (i % 4) + (i >= 4 ? "G" : "");
            Assert.That(weapons, Does.Contain("modular.custom.w" + i + ":\n\tInherits: ChinaMGatt." + suffix + "\n"));
            Assert.That(rules, Does.Contain("Armament@GAT" + suffix + ":\n\t\tWeapon: modular.custom.w" + i + "\n"));
        }
        foreach (var field in new[] { "RequiresCondition:", "ReloadingCondition:", "GrantConditionOnAttackCA", "PauseOnCondition:" })
            Assert.That(rules, Does.Not.Contain(field));
        Assert.That(weapons, Does.Not.Contain("ReloadDelay:"));
        Assert.That(rules, Does.Contain("~!promotion.gatling.pdl, ~!promotion.gatling.reflector"));
    }

    [Test]
    public void OverlordPreservesEmperorButAlsoUsesCustomNameInConditionalTooltip()
    {
        var c = Compiler; var p = Design(c, "overlord-chassis"); p.TankName = "Mein schwerer Panzer";
        var rules = c.Rules(p);
        Assert.That(rules, Does.Contain("Tooltip@Emperor:\n\t\tName: Mein schwerer Panzer\n"));
        Assert.That(rules, Does.Contain("Tooltip:\n\t\tName: Mein schwerer Panzer\n"));
        Assert.That(rules, Does.Not.Contain("DamageMultiplier@uparmor"));
        Assert.That(rules, Does.Not.Contain("WithVoxelBody@emperor"));
        Assert.That(rules, Does.Contain("Locomotor: sheavytracked"));
        Assert.That(rules, Does.Contain("radar, ~vehicles.china, ~!vehicles.chinainf, ~!upg.overlord"));
        Assert.That(c.WeaponSummary(p), Does.Contain("80%"));
        Assert.That(c.Weapons(p), Is.EqualTo("modular.custom.w0:\n\tInherits: OverlordCannon\n"));
    }

    [Test]
    public void ChinaCannotMixNodGdiOrStationaryAndCopiesKeepTheirBase()
    {
        var c = Compiler; var r = c.NewRoster("chinatnk"); var p = c.Profile(r, r.Designs[0]);
        Assert.That(c.CompatibleOptions(p, "chassis").Length, Is.EqualTo(8));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "chassis", "nod-light-hull"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "running_gear", "gdi-stationary"));
        var d = c.AddDesign(r); var copy = c.AddDesign(r, d);
        c.SelectPart(c.Profile(r, copy), "chassis", "dragon-chassis");
        Assert.That(d.Parts["chassis"], Is.EqualTo("battlemaster"));
        Assert.That(c.DeserializeRoster(c.SerializeRoster(r)).BaseFaction, Is.EqualTo("chinatnk"));
        p.BaseFaction = "eagle";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [Test]
    public void FourFamiliesFreezeInChinaMapWithMatchingReferenceAndSafeTemplateNames()
    {
        var c = Compiler; var r = c.NewRoster("chinatnk");
        foreach (var hull in c.CompatibleOptions(c.Profile(r, r.Designs[0]), "chassis").Skip(1))
        {
            var d = c.AddDesign(r); c.SelectPart(c.Profile(r, d), "chassis", hull);
            d.Name = "Eigen " + c.Label(hull).Split(new[] { " - " }, StringSplitOptions.None)[0];
        }
        Assert.That(c.ValidateRoster(r), Is.LessThanOrEqualTo(50));
        var lab = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
        var bytes = c.CompileRosterMap(r, lab);
        Assert.That(c.CompileRosterMap(r, lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes)); var map = Read(zip, "map.yaml");
        foreach (var player in new[] { "Multi0", "Multi1" })
            Assert.That(map, Does.Contain("PlayerReference@" + player + ":\n\t\tName: " + player + "\n\t\tPlayable: True\n\t\tLockFaction: True\n\t\tFaction: chinatnk"));
        Assert.That(map, Does.Contain("Reference: chbattle"));
        Assert.That(map, Does.Not.Contain("Transport: ocar"));
        Assert.That(Read(zip, "modular-rules.yaml"), Does.Contain("FactionCA@22"));
        Assert.That(c.DeserializeRoster(Read(zip, "custom-faction.json")).Designs.Count, Is.EqualTo(8));
        var path = Environment.GetEnvironmentVariable("MODULAR_CHINA_EXPORT");
        if (!string.IsNullOrEmpty(path)) { using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes); }
    }
}
