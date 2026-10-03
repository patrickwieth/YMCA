using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class SharedGearAndNodCombatTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));
    static CustomFactionDesign Compiler => new(Catalog);

    [TestCase("eagle", "designer-mlrs-hull", "designer-mlrs-gear", 950, 16000, 82)]
    [TestCase("blackh", "designer-ssm-hull", "designer-ssm-gear", 1050, 15000, 82)]
    [TestCase("england", "designer-prism-hull", "designer-prism-gear", 1350, 22000, 82)]
    public void LegacySavedProfilesMigrateOnlyEquivalentGearWithoutChangingIdentityOrBaseline(string faction, string hull, string oldGear, int cost, int hp, int speed)
    {
        var c = Compiler; var roster = c.NewRoster(faction); var p = c.Profile(roster, roster.Designs[0]);
        c.SelectPart(p, "chassis", hull);
        Assert.That(p.Parts["running_gear"], Is.EqualTo("light-tracks"));
        var old = c.SerializeRoster(roster).Replace("\"light-tracks\"", "\"" + oldGear + "\"");
        var loaded = c.DeserializeRoster(old);
        Assert.That(loaded.Designs[0].Id, Is.EqualTo(roster.Designs[0].Id));
        Assert.That(loaded.Designs[0].Name, Is.EqualTo(roster.Designs[0].Name));
        Assert.That(loaded.Name, Is.EqualTo(roster.Name));
        Assert.That(c.SerializeRoster(loaded), Is.EqualTo(c.SerializeRoster(roster)));
        var legacySingle = c.Deserialize(c.Serialize(p).Replace("\"light-tracks\"", "\"" + oldGear + "\""));
        var v = c.Calculate(legacySingle);
        Assert.That(v.Cost, Is.EqualTo(cost)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(v.Turn, Is.EqualTo(48));
        Assert.That(c.Rules(legacySingle), Does.Contain("Locomotor: wheeled"));
    }

    [Test]
    public void CompatibilityClassDoesNotSilentlyChangeMovementOrAdmitArbitraryAssemblies()
    {
        var c = Compiler; var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", "designer-mlrs-hull");
        Assert.That(c.Label("light-tracks"), Is.EqualTo("Light Tracks"));
        Assert.That(c.Options("running_gear").Length, Is.EqualTo(16));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "running_gear", "tracks-light-artillery"));
        var data = JObject.Parse(Catalog); data["components"]["light-tracks"]["compatibility_class"] = "heavy-tracks";
        var invalid = new CustomFactionDesign(data.ToString());
        Assert.Throws<InvalidDataException>(() => invalid.Calculate(p));
        var json = c.Serialize(p).Replace("\"light-tracks\"", "\"unknown-tracks\"");
        Assert.Throws<InvalidDataException>(() => c.Deserialize(json));
    }

    [Test]
    public void NodComplexPackagesInheritAllBehaviorWithoutGenericTraitReplacements()
    {
        var c = Compiler; var r = c.NewRoster("blackh"); var p = c.Profile(r, r.Designs[0]);
        foreach (var hull in c.CompatibleOptions(p, "chassis").Where(h => h.StartsWith("nod-combat-")))
        {
            c.SelectPart(p, "chassis", hull);
            var rules = c.Rules(p);
            Assert.That(rules, Does.Not.Contain("\tCarryable:"));
            Assert.That(rules, Does.Not.Contain("\tCargo:"));
            Assert.That(rules, Does.Not.Contain("\tCloak"));
            Assert.That(rules, Does.Not.Contain("\tGrantConditionOnDeploy:"));
            Assert.That(rules, Does.Not.Contain("\tTurreted:"));
            Assert.That(rules, Does.Not.Contain("\tFireWarheadsOnDeathCA"));
            Assert.That(c.Weapons(p), Does.Not.Contain("Damage:"));
            Assert.That(c.Weapons(p), Does.Not.Contain("ReloadDelay:"));
            Assert.That(c.CompatibleOptions(p, "carrier"), Is.EqualTo(new[] { "integrated-mount" }));
            Assert.That(c.CompatibleOptions(p, "ammunition"), Is.EqualTo(new[] { "integral-stores" }));
        }
        c.SelectPart(p, "chassis", "nod-combat-beam-hull");
        Assert.That(c.Weapons(p).Split("Inherits:").Length - 1, Is.EqualTo(8));
        Assert.That(c.Weapons(p), Does.Contain("BeamCannonCharge"));
        Assert.That(c.Rules(p), Does.Contain("tmpl, vehicles, ~promotion.beam_cannon, ~structures.blackh"));
        c.SelectPart(p, "chassis", "nod-combat-heavy-flame-hull");
        Assert.That(c.Rules(p), Does.Contain("radar, vehicles, ~!upg.flametank, ~structures.blackh"));
        Assert.That(c.Rules(p), Does.Not.Contain(", tier3"));
        c.SelectPart(p, "chassis", "nod-combat-chemical-hull");
        Assert.That(c.Weapons(p), Does.Contain("DemoTruckTargeting"));
    }
}
