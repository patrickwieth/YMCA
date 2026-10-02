using System.IO.Compression;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomFactionDesignTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));
    static byte[] Lab => File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
    static CustomFactionDesign Compiler => new(Catalog);

    [TestCase("tracks-standard", 900, 82)]
    [TestCase("prototype-hover", 1100, 79)]
    [TestCase("gdi-stationary", 850, 0)]
    public void DefaultsMatchOfflinePrototype(string gear, double cost, int speed)
    {
        var p = new CustomFactionProfile(); p.Parts["running_gear"] = gear;
        var values = Compiler.Calculate(p);
        Assert.That(values.Cost, Is.EqualTo(cost));
        Assert.That(values.Hp, Is.EqualTo(52000));
        Assert.That(values.Speed, Is.EqualTo(speed));
        Assert.That(values.Mass, Is.GreaterThan(0));
    }

    [Test]
    public void AllOfferedCombinationsCompileAndUseFullPayloadTemplate()
    {
        var compiler = Compiler;
        foreach (var gear in compiler.CompatibleOptions(new CustomFactionProfile(), "running_gear"))
        foreach (var drive in compiler.CompatibleOptions(new CustomFactionProfile(), "drive"))
        foreach (var generator in compiler.CompatibleOptions(new CustomFactionProfile(), "generator"))
        foreach (var ammo in compiler.CompatibleOptions(new CustomFactionProfile(), "ammunition"))
        {
            var p = new CustomFactionProfile();
            p.Parts["running_gear"] = gear; p.Parts["drive"] = drive;
            p.Parts["generator"] = generator; p.Parts["ammunition"] = ammo;
            var values = compiler.Calculate(p);
            Assert.That(values.Cost, Is.GreaterThan(0));
            Assert.That(compiler.Weapons(p), Does.Contain(ammo == "designer-he-shell" ? "Inherits: 120mmHEAT" : "Inherits: 120mm\n"));
            if (values.Stationary)
            {
                Assert.That(compiler.Rules(p), Does.Contain("\t-Buildable:"));
                Assert.That(values.Turn, Is.Zero);
            }
            else
            {
                Assert.That(compiler.Rules(p), Does.Contain("~structures.eagle"));
                if (values.Tech > 1) Assert.That(compiler.Rules(p), Does.Contain(", tier" + values.Tech));
            }
        }
    }

    [Test]
    public void AllNativeCalculationsMatchTheIndependentPythonCalculator()
    {
        var rows = JArray.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-calculation-cases.json")));
        Assert.That(rows.Count, Is.EqualTo(124));
        foreach (var row in rows)
        {
            var profile = new CustomFactionProfile { BaseFaction = row.Value<string>("base_faction"), Parts = row["parts"].ToObject<Dictionary<string, string>>() };
            var v = Compiler.Calculate(profile); var expected = row["values"];
            Assert.That(v.Cost, Is.EqualTo(expected.Value<double>("cost")).Within(0.00001));
            Assert.That(v.Mass, Is.EqualTo(expected.Value<double>("mass")).Within(0.00001));
            Assert.That(v.Hp, Is.EqualTo(expected.Value<double>("hp")).Within(0.00001));
            Assert.That(v.Electric, Is.EqualTo(expected.Value<double>("electric_kw")).Within(0.00001));
            Assert.That(v.Reserve, Is.EqualTo(expected.Value<double>("reserve_kw")).Within(0.00001));
            Assert.That(v.Speed, Is.EqualTo(expected.Value<int>("speed")));
            Assert.That(v.Turn, Is.EqualTo(expected.Value<int>("turn_speed")));
            Assert.That(v.Tech, Is.EqualTo(expected.Value<int>("tech")));
            Assert.That(v.Points, Is.EqualTo(expected.Value<int>("catalog_points")));
        }
    }

    [Test]
    public void UnknownPartsFactionAndUnsafeNamesFailClosed()
    {
        var p = new CustomFactionProfile(); p.Parts["armor"] = "reflector";
        Assert.Throws<InvalidDataException>(() => Compiler.Calculate(p));
        p = new CustomFactionProfile { BaseFaction = "nod" };
        Assert.Throws<InvalidDataException>(() => Compiler.Calculate(p));
        p = new CustomFactionProfile { Name = "Injected:\nWorld:" };
        Assert.Throws<InvalidDataException>(() => Compiler.Calculate(p));
        p = new CustomFactionProfile(); p.Parts["equipment"] = "pdl";
        Assert.Throws<InvalidDataException>(() => Compiler.Calculate(p));
        Assert.Throws<InvalidDataException>(() => Compiler.Deserialize("null"));
        Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => Compiler.Deserialize("{\"HiddenDiscount\":42}"));
    }

    [Test]
    public void CannotEnableNewBehaviorOnlyByExtendingDataOptions()
    {
        var data = JObject.Parse(Catalog);
        ((JArray)data["options"]["armor"]).Add("reflector");
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()));
        data = JObject.Parse(Catalog);
        data["components"]["gdi-stationary"]["locomotor"] = "immobile";
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()));
    }

    [Test]
    public void SavesRoundtripAndKeepsPreviousProfileBackup()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var path = Path.Combine(dir, "profile.json");
            var c = Compiler; var p = new CustomFactionProfile();
            c.Save(path, p); var original = File.ReadAllText(path);
            p.Name = "Andere GDI"; p.Parts["running_gear"] = "prototype-hover";
            c.Save(path, p);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(original));
            var loaded = c.Deserialize(File.ReadAllText(path));
            Assert.That(loaded.Name, Is.EqualTo(p.Name));
            Assert.That(loaded.Parts, Is.EquivalentTo(p.Parts));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Test]
    public void MapIsFrozenAndKeepsRosterPlusOneCustomActor()
    {
        var c = Compiler; var p = new CustomFactionProfile();
        var bytes = c.CompileMap(p, Lab);
        Assert.That(c.CompileMap(p, Lab), Is.EqualTo(bytes));
        p.Parts["ammunition"] = "designer-he-shell";
        var changed = c.CompileMap(p, Lab);
        Assert.That(CustomFactionDesign.ContentHash(changed), Is.Not.EqualTo(CustomFactionDesign.ContentHash(bytes)));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        string Read(string name) { using var r = new StreamReader(zip.GetEntry(name).Open()); return r.ReadToEnd(); }
        Assert.That(Read("map.yaml"), Does.Contain("CustomTank: modular.custom"));
        Assert.That(Read("map.yaml"), Does.Not.Contain("Prototype0:"));
        Assert.That(Read("map.yaml"), Does.Not.Contain("Prototype1:"));
        Assert.That(Read("map.yaml"), Does.Not.Contain("Prototype2:"));
        Assert.That(Read("modular-rules.yaml"), Does.Contain("Inherits: MTNK"));
        Assert.That(Read("modular-rules.yaml"), Does.Contain("FactionCA@11:"));
        Assert.That(Read("custom-faction.json"), Does.Contain("medium-tank-shell"));
        Assert.That(zip.GetEntry("modular-manifest.json"), Is.Null, "Old lab manifest must not describe the new design.");
    }

    [Test]
    public void CanExportNativeSampleForEngineValidation()
    {
        var destination = Environment.GetEnvironmentVariable("MODULAR_TEST_EXPORT");
        if (string.IsNullOrEmpty(destination)) return;
        var p = new CustomFactionProfile { Name = "Native Designer Test" };
        p.Parts["ammunition"] = "designer-he-shell";
        using var stream = new FileStream(destination, FileMode.CreateNew);
        stream.Write(Compiler.CompileMap(p, Lab));
    }
}
