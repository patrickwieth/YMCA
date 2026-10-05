using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class TurretModuleTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));

    [TestCase("england", "designer-prism-hull", "designer-prism-mount", "prism-turret", "Prism Turret", 1350, 22000, 82)]
    [TestCase("blackh", "nod-combat-howitzer-hull", "integrated-mount", "artillery-turret", "Artillery Turret", 550, 15000, 68)]
    [TestCase("blackh", "nod-combat-stealth-hull", "integrated-mount", "missile-turret", "Missile Turret", 1200, 20000, 135)]
    [TestCase("eagle", "designer-disruptor-hull", "designer-disruptor-mount", "sonic-turret", "Sonic Turret", 1500, 75000, 56)]
    public void NamedModulesPreserveBaselineAndMigrateExistingProfiles(string faction, string hull, string oldId, string module, string label, int cost, int hp, int speed)
    {
        var c = new CustomFactionDesign(Catalog); var r = c.NewRoster(faction); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", hull);
        Assert.That(p.Parts["carrier"], Is.EqualTo(module)); Assert.That(c.Label(module), Is.EqualTo(label));
        var v = c.Calculate(p); Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(c.WeaponSummary(p), Does.StartWith(label + ": Turret slots: 1/1 occupied."));
        var legacy = c.SerializeRoster(r).Replace("\"" + module + "\"", "\"" + oldId + "\"");
        var loaded = c.DeserializeRoster(legacy);
        Assert.That(c.SerializeRoster(loaded), Is.EqualTo(c.SerializeRoster(r)));
        var oldProfile = c.Serialize(p).Replace("\"" + module + "\"", "\"" + oldId + "\"");
        var migrated = c.Deserialize(oldProfile);
        Assert.That(c.Rules(migrated), Is.EqualTo(c.Rules(p))); Assert.That(c.Weapons(migrated), Is.EqualTo(c.Weapons(p)));
        Assert.That(c.Rules(p), Does.Not.Contain("\tTurreted:"), "Keep original geometry and turn rate, not generic scalar mount overrides.");
    }

    [Test]
    public void RecipesCannotEnableUnreviewedBattleFortressMixesOrSilentlyRebindSourceActors()
    {
        var c = new CustomFactionDesign(Catalog); var r = c.NewRoster("england"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", "stock-batf-hull");
        foreach (var turret in new[] { "prism-turret", "artillery-turret", "missile-turret", "sonic-turret" })
            Assert.Throws<ArgumentException>(() => c.SelectPart(p, "carrier", turret));
        c.SelectPart(p, "chassis", "designer-prism-hull");
        var data = JObject.Parse(Catalog); data["turret_modules"]["prism-turret"]["source_actor"] = "BATF.Prism";
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()).Calculate(p));
        data = JObject.Parse(Catalog); data["turret_modules"]["prism-turret"]["arbitrary_mounting"] = true;
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()).Calculate(p));
    }

    [Test]
    public void IntegratedMountMigrationIsChassisSpecificAndSonicKeepsAllFourChannels()
    {
        var c = new CustomFactionDesign(Catalog); var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", "nod-combat-apc-hull");
        Assert.That(c.Deserialize(c.Serialize(p)).Parts["carrier"], Is.EqualTo("integrated-mount"));
        r = c.NewRoster("eagle"); p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", "designer-disruptor-hull");
        var weapons = c.Weapons(p);
        foreach (var name in new[] { "SonicZap", "SonicZapVisual", "SonicZap.UPG", "SonicZapVisual.UPG" })
            Assert.That(weapons, Does.Contain("Inherits: " + name + "\n"));
    }
}
