using System.IO.Compression;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomFactionRosterTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));
    static byte[] Lab => File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
    static CustomFactionDesign Compiler => new(Catalog);
    static CustomFactionRoster Fleet(CustomFactionDesign c)
    {
        var r = new CustomFactionRoster();
        foreach (var hull in new[] { "humvee-hull", "designer-mlrs-hull" })
        {
            var d = c.AddDesign(r);
            c.SelectPart(c.Profile(r, d), "chassis", hull);
        }
        return r;
    }

    [Test]
    public void LegacyProfileMigratesWithoutChangingNamePartsOrNumbers()
    {
        var c = Compiler; var old = new CustomFactionProfile { Name = "Meine alte GDI", TankName = "Meine Plattform" };
        old.Parts["running_gear"] = "gdi-stationary"; old.Parts["generator"] = "efficient-generator";
        old.Parts["ammunition"] = "designer-he-shell";
        var migrated = c.DeserializeRoster(c.Serialize(old));
        Assert.That(migrated.Schema, Is.EqualTo(2));
        Assert.That(migrated.Name, Is.EqualTo(old.Name));
        Assert.That(migrated.Designs.Single().Name, Is.EqualTo(old.TankName));
        Assert.That(migrated.Designs.Single().Parts, Is.EquivalentTo(old.Parts));
        Assert.That(c.Calculate(c.Profile(migrated, migrated.Designs[0])).Cost, Is.EqualTo(1075));
        Assert.That(c.DeserializeRoster(c.SerializeRoster(migrated)).Designs[0].Id, Is.EqualTo("tank1"));
    }

    [TestCase("humvee-hull", 400, 15000, 157, "HMMV", "hmmv")]
    [TestCase("designer-mlrs-hull", 950, 16000, 82, "MLRS", "mlrs")]
    public void NewAssembliesHaveExplicitGraphicsAndReferenceBaselines(string hull, int cost, int hp, int speed, string actor, string image)
    {
        var c = Compiler; var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", hull);
        var values = c.Calculate(p);
        Assert.That(values.Cost, Is.EqualTo(cost)); Assert.That(values.Hp, Is.EqualTo(hp)); Assert.That(values.Speed, Is.EqualTo(speed));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.Rules(p), Does.Contain("Image: " + image + "\n"));
        Assert.That(c.Rules(p), Does.Contain("Type: Light"));
    }

    [Test]
    public void MissileBindingPreservesBothWeaponsAndTheirIndependentBehavior()
    {
        var c = Compiler; var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", "designer-mlrs-hull");
        var rules = c.Rules(p); var weapons = c.Weapons(p);
        Assert.That(rules, Does.Contain("Armament@PRIMARY:\n\t\tWeapon: modular.custom.gun"));
        Assert.That(rules, Does.Contain("Armament@SECONDARY:\n\t\tWeapon: modular.custom.aa"));
        Assert.That(weapons, Does.Contain("modular.custom.gun:\n\tInherits: 227mm\n\tReloadDelay: 120"));
        Assert.That(weapons, Does.Contain("modular.custom.aa:\n\tInherits: 227mmAA\n\tReloadDelay: 80"));
        Assert.That(weapons, Does.Contain("MinRange: 4c0"));
        Assert.That(weapons, Does.Contain("Range: 10c512"));
        Assert.That(weapons, Does.Not.Contain("Projectile:"), "Complete inherited projectile behavior must remain intact.");
        Assert.That(rules, Does.Not.Contain("-Armament"));
        Assert.That(rules, Does.Not.Contain("-AttackFrontal"));
        Assert.That(c.CompatibleOptions(p, "running_gear"), Does.Not.Contain("gdi-stationary"), "Frontal launcher cannot fire with a permanently locked hull.");
        p.Parts["running_gear"] = "gdi-stationary";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [Test]
    public void MachineGunUsesRealHumveeMountAndChargesMandatorySensorsOnce()
    {
        var c = Compiler; var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", "humvee-hull");
        Assert.That(c.Weapons(p), Does.Contain("modular.custom.gun:\n\tInherits: M60mgTD\n\tReloadDelay: 30\n\tBurst: 5"));
        Assert.That(c.Rules(p), Does.Contain("\tArmament:\n\t\tWeapon: modular.custom.gun"));
        Assert.That(c.Rules(p), Does.Not.Contain("Armament@PRIMARY"));
        Assert.That(c.Calculate(p).Mass, Is.EqualTo(2500));
        Assert.That(c.Calculate(p).Electric, Is.EqualTo(10));
        Assert.That(c.Calculate(p).Cost, Is.EqualTo(400));
        p.Parts["weapon"] = "medium-cannon";
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [Test]
    public void ComponentScalarsRemainAuthoritativeForBothMissileChannels()
    {
        var data = JObject.Parse(Catalog);
        data["components"]["designer-rocket-mount"]["secondary_reload_ticks"] = 75;
        data["components"]["designer-rocket-mount"]["fire_delay_ticks"] = 15;
        data["components"]["designer-rocket-payload"]["secondary_damage"] = 3333;
        var c = new CustomFactionDesign(data.ToString()); var p = new CustomFactionProfile();
        c.SelectPart(p, "chassis", "designer-mlrs-hull");
        var weapons = c.Weapons(p);
        Assert.That(weapons, Does.Contain("ReloadDelay: 120"));
        Assert.That(weapons, Does.Contain("ReloadDelay: 75"));
        Assert.That(weapons, Does.Contain("Damage: 1300"));
        Assert.That(weapons, Does.Contain("Damage: 3333"));
        Assert.That(c.Rules(p).Split("FireDelay: 15").Length - 1, Is.EqualTo(2));
        Assert.That(weapons, Does.Contain("Inherits: 227mmAA"));
    }

    [Test]
    public void CopyDoesNotSharePartsOrActorIdsAndNamesMustBeUnique()
    {
        var c = Compiler; var r = new CustomFactionRoster(); var original = r.Designs[0];
        var copy = c.AddDesign(r, original);
        Assert.That(copy.Id, Is.Not.EqualTo(original.Id));
        Assert.That(copy.Name, Is.Not.EqualTo(original.Name));
        c.SelectPart(c.Profile(r, copy), "chassis", "humvee-hull");
        Assert.That(original.Parts["chassis"], Is.EqualTo("gdi-battle-hull"));
        copy.Name = original.Name.ToLowerInvariant();
        Assert.Throws<InvalidDataException>(() => c.ValidateRoster(r));
        copy.Name = "Unique name"; copy.Id = original.Id;
        Assert.Throws<InvalidDataException>(() => c.ValidateRoster(r));
        copy.Id = "bad:\nActor";
        Assert.Throws<InvalidDataException>(() => c.ValidateRoster(r));
    }

    [Test]
    public void SharedBudgetAndSizeAreEnforcedBeforeWriting()
    {
        var data = JObject.Parse(Catalog); data["components"]["diesel"]["tier"] = 3;
        var c = new CustomFactionDesign(data.ToString()); var r = new CustomFactionRoster();
        while (r.Designs.Count < 12) c.AddDesign(r);
        Assert.That(c.ValidateRoster(r), Is.EqualTo(48));
        Assert.Throws<InvalidDataException>(() => c.AddDesign(r));
        Assert.That(r.Designs.Count, Is.EqualTo(12), "Rejected addition must roll back.");
        c = Compiler; r = new CustomFactionRoster();
        while (r.Designs.Count < CustomFactionDesign.MaxDesigns) c.AddDesign(r);
        Assert.Throws<InvalidDataException>(() => c.AddDesign(r));
        r.Designs.Clear(); Assert.Throws<InvalidDataException>(() => c.ValidateRoster(r));
    }

    [Test]
    public void EveryActorAndWeaponIsNamespacedAndSnapshotIsImmutable()
    {
        var c = Compiler; var r = Fleet(c); var bytes = c.CompileRosterMap(r, Lab);
        Assert.That(c.CompileRosterMap(r, Lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        string Read(string name) { using var reader = new StreamReader(zip.GetEntry(name).Open()); return reader.ReadToEnd(); }
        var rules = Read("modular-rules.yaml"); var weapons = Read("modular-weapons.yaml"); var map = Read("map.yaml");
        foreach (var d in r.Designs)
        {
            var id = "modular.custom." + d.Id;
            Assert.That(rules, Does.Contain(id + ":\n"));
            Assert.That(weapons, Does.Contain(id + ".gun:\n"));
            Assert.That(map, Does.Contain(": " + id + "\n"));
        }
        var missile = "modular.custom." + r.Designs[2].Id;
        Assert.That(weapons, Does.Contain(missile + ".aa:\n\tInherits: 227mmAA"));
        Assert.That(map, Does.Not.Contain("Prototype0:"));
        Assert.That(map, Does.Contain("Reference: mtnk"));
        Assert.That(c.DeserializeRoster(Read("custom-faction.json")).Designs.Count, Is.EqualTo(3));
        var originalId = r.Designs[0].Id;
        r.Designs.Reverse();
        Assert.That(c.SerializeRoster(r), Does.Contain(originalId), "Reordering must not rename actor types.");
        r.Designs[0].Name = "Changed after freeze";
        Assert.That(c.CompileRosterMap(r, Lab), Is.Not.EqualTo(bytes));
        Assert.That(Read("custom-faction.json"), Does.Not.Contain("Changed after freeze"));
    }

    [Test]
    public void NamedLibraryAndCurrentProfileHaveIndependentBackups()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var c = Compiler; var r = Fleet(c);
            var path = Path.Combine(directory, "active.json"); var library = Path.Combine(directory, "Factions");
            c.SaveRoster(path, r); c.SaveLibrary(library, r); var old = File.ReadAllText(path);
            r.Designs[0].Name = "Renamed tank";
            c.SaveRoster(path, r); c.SaveLibrary(library, r);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(old));
            var saved = Directory.GetFiles(library, "*.json").Single();
            Assert.That(File.ReadAllText(saved + ".bak"), Is.EqualTo(old));
            r.Name = "Second faction"; c.SaveLibrary(library, r);
            Assert.That(Directory.GetFiles(library, "*.json").Length, Is.EqualTo(2));
            Assert.That(c.DeserializeRoster(File.ReadAllText(saved)).Designs[0].Name, Is.EqualTo("Renamed tank"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void RejectsUnknownSchemaAndHiddenRosterFields()
    {
        var c = Compiler;
        Assert.Throws<InvalidDataException>(() => c.DeserializeRoster("{\"Schema\":99}"));
        Assert.Throws<InvalidDataException>(() => c.DeserializeRoster("{\"Schema\":2}"));
        var root = JObject.Parse(c.SerializeRoster(new CustomFactionRoster()));
        root["Discount"] = 100;
        Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => c.DeserializeRoster(root.ToString()));
    }

    [Test]
    public void IncompleteLoadedDesignsAreNotFilledWithConstructorDefaults()
    {
        var c = Compiler; var root = JObject.Parse(c.SerializeRoster(new CustomFactionRoster()));
        ((JObject)root["Designs"][0]["Parts"]).Remove("drive");
        Assert.Throws<InvalidDataException>(() => c.DeserializeRoster(root.ToString()));
        root = JObject.Parse(c.SerializeRoster(new CustomFactionRoster()));
        ((JObject)root["Designs"][0]).Remove("Id");
        Assert.Throws<InvalidDataException>(() => c.DeserializeRoster(root.ToString()));
    }

    [Test]
    public void DataCannotSilentlyEnableAnUnboundGraphicsCombination()
    {
        var data = JObject.Parse(Catalog);
        ((JArray)data["assemblies"][1]["options"]["weapon"]).Add("medium-cannon");
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()));
    }

    [Test]
    public void ExportRosterSampleForEngineValidation()
    {
        var destination = Environment.GetEnvironmentVariable("MODULAR_ROSTER_EXPORT");
        if (string.IsNullOrEmpty(destination)) return;
        var c = Compiler; var r = Fleet(c);
        // Include stationary and hover bindings alongside all three normal assemblies.
        var stationary = c.AddDesign(r, r.Designs[0]); stationary.Parts["running_gear"] = "gdi-stationary";
        var hover = c.AddDesign(r, r.Designs[0]); hover.Parts["running_gear"] = "prototype-hover";
        var stationaryMg = c.AddDesign(r, r.Designs[1]); stationaryMg.Parts["running_gear"] = "gdi-stationary";
        using var stream = new FileStream(destination, FileMode.CreateNew);
        stream.Write(c.CompileRosterMap(r, Lab));
    }
}
