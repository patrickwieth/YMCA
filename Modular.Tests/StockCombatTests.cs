using System.IO.Compression;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class StockCombatTests
{
    static string DirectoryRoot => TestContext.CurrentContext.TestDirectory;
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(DirectoryRoot, "designer-catalog.json")));
    static JArray Rows => JArray.Parse(File.ReadAllText(Path.Combine(DirectoryRoot, "stock-combat-baselines.json")));
    static readonly Dictionary<string, string> Bases = new() { { "gdi", "eagle" }, { "allies", "england" }, { "scrin", "traveler" }, { "soviet", "russia" } };
    static string Hull(JToken row) => row.Value<string>("actor") == "BATF.Bunker" ? "stock-batf-hull" : "stock-" + row.Value<string>("actor").ToLowerInvariant().Replace('_', '-').Replace('.', '-') + "-hull";
    public static IEnumerable<TestCaseData> StockCases => Rows.Select(r => new TestCaseData(r.ToString()).SetName("StockCombat_" + r.Value<string>("actor")));

    [TestCaseSource(nameof(StockCases))]
    public void ExactBaseValuesAndUntouchedStockPackages(string json)
    {
        var row = JObject.Parse(json); var c = Compiler; var roster = c.NewRoster(Bases[row.Value<string>("faction")]);
        var p = c.Profile(roster, roster.Designs[0]); c.SelectPart(p, "chassis", Hull(row));
        var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(row.Value<int>("cost"))); Assert.That(v.Hp, Is.EqualTo(row.Value<int>("hp")));
        Assert.That(v.Speed, Is.EqualTo(row.Value<int>("speed"))); Assert.That(v.Turn, Is.EqualTo(row.Value<int>("turn")));
        Assert.That(c.UsesStockArmaments(p), Is.True);
        Assert.That(c.Weapons(p), Is.Empty, "Whole stock packages must not be reconstructed as fake generic guns.");
        var rules = c.Rules(p);
        Assert.That(rules, Does.Contain("Inherits: " + row.Value<string>("actor") + "\n"));
        Assert.That(rules, Does.Contain("Locomotor: " + row.Value<string>("locomotor") + "\n"));
        Assert.That(rules, Does.Contain("Type: " + row.Value<string>("armor") + "\n"));
        foreach (var trait in new[] { "Armament", "Cargo", "RenderSprites", "RenderVoxels", "Transforms", "Carryable", "GrantConditionOnDeploy", "Turreted" })
            Assert.That(rules, Does.Not.Contain("\t" + trait + ":"));
        foreach (var requirement in row["prerequisites"].Values<string>().Where(r => r.StartsWith("~promotion.") || r.StartsWith("~!upg.") || r.StartsWith("~!promotion.") || !r.StartsWith("~")))
            Assert.That(rules, Does.Contain(requirement));
        Assert.That(rules, Does.Not.Contain("~disabled"));
        Assert.That(c.Deserialize(c.Serialize(p)).Parts, Is.EquivalentTo(p.Parts));
    }

    [TestCase("eagle", 15, 0)]
    [TestCase("blackh", 14, 0)]
    [TestCase("chinatnk", 8, 0)]
    [TestCase("england", 13, 0)]
    [TestCase("russia", 16, 9)]
    [TestCase("traveler", 14, 0)]
    public void TemplateButtonFillsWithoutLosingExistingDesignsOrExceedingRosterLimit(string faction, int count, int remaining)
    {
        var c = Compiler; var r = c.NewRoster(faction); var first = c.Serialize(c.Profile(r, r.Designs[0])); var id = r.Designs[0].Id;
        Assert.That(c.AddTemplates(r), Is.EqualTo(remaining));
        Assert.That(r.Designs.Count, Is.EqualTo(count)); Assert.That(c.ValidateRoster(r), Is.LessThanOrEqualTo(50));
        Assert.That(r.Designs[0].Id, Is.EqualTo(id)); Assert.That(c.Serialize(c.Profile(r, r.Designs[0])), Is.EqualTo(first));
        var saved = c.SerializeRoster(r); c.AddTemplates(r); Assert.That(c.SerializeRoster(r), Is.EqualTo(saved));
        if (remaining > 0)
        {
            var missing = c.CompatibleOptions(c.Profile(r, r.Designs[0]), "chassis").First(h => r.Designs.All(d => d.Parts["chassis"] != h));
            c.SelectPart(c.Profile(r, r.Designs[^1]), "chassis", missing);
            Assert.DoesNotThrow(() => c.ValidateRoster(r));
        }
    }

    [Test]
    public void BattleFortressRequiresASuperheavyCompatibleBunkerModule()
    {
        var c = Compiler; var r = c.NewRoster("england"); var p = c.Profile(r, r.Designs[0]);
        c.SelectPart(p, "chassis", "stock-batf-hull");
        Assert.That(c.Label(p.Parts["chassis"]), Is.EqualTo("Battle Fortress"));
        Assert.That(p.Parts["carrier"], Is.EqualTo("superheavy-bunker"));
        Assert.That(c.Label(p.Parts["carrier"]), Is.EqualTo("Bunker Module"));
        Assert.That(c.Calculate(p).Cost, Is.EqualTo(3000));
        Assert.That(c.Rules(p), Does.Contain("Inherits: BATF.Bunker\n"));
        Assert.That(c.Rules(p), Does.Contain("~promotion.battle_fortress.bunker"));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "carrier", "integrated-mount"));
        var catalog = JObject.Parse(File.ReadAllText(Path.Combine(DirectoryRoot, "designer-catalog.json")));
        Assert.That(catalog["components"]["stock-batf-hull"].Value<string>("chassis_class"), Is.EqualTo("superheavy"));
        Assert.That(catalog["components"]["superheavy-bunker"].Value<int>("cost"), Is.EqualTo(1000));
        Assert.That(catalog["components"]["stock-batf-hull"].Value<int>("carrier_slots"), Is.EqualTo(3));
        Assert.That(catalog["components"]["superheavy-bunker"].Value<int>("slots_required"), Is.EqualTo(3));
        Assert.That(c.WeaponSummary(p), Does.StartWith("Turret slots: 3/3 occupied."));
        catalog["components"]["stock-batf-hull"]["chassis_class"] = "standard";
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(catalog.ToString()).Calculate(p));
        var tank = new CustomFactionProfile();
        Assert.Throws<ArgumentException>(() => c.SelectPart(tank, "carrier", "superheavy-bunker"));
    }

    [TestCase("carrier_slots", 2)]
    [TestCase("carrier_slots", 0)]
    [TestCase("carrier_slots", 3.5)]
    [TestCase("slots_required", 4)]
    [TestCase("slots_required", 0)]
    [TestCase("slots_required", -1)]
    [TestCase("slots_required", 1.5)]
    public void InvalidBunkerSlotAllocationsAreRejectedNatively(string field, double value)
    {
        var data = JObject.Parse(File.ReadAllText(Path.Combine(DirectoryRoot, "designer-catalog.json")));
        data["components"][field == "carrier_slots" ? "stock-batf-hull" : "superheavy-bunker"][field] = value;
        var c = new CustomFactionDesign(data.ToString()); var r = c.NewRoster("england");
        var p = c.Profile(r, r.Designs[0]); c.SelectPart(p, "chassis", "stock-batf-hull");
        Assert.Throws<InvalidDataException>(() => c.Calculate(p));
    }

    [TestCase("gdi")]
    [TestCase("allies")]
    [TestCase("soviet")]
    [TestCase("scrin")]
    public void EveryNewFamilyFreezesInBatchesIncludingAnEmptyLocalWeaponsFile(string faction)
    {
        var c = Compiler; var rows = Rows.Where(r => r.Value<string>("faction") == faction).ToArray();
        for (var offset = 0; offset < rows.Length; offset += CustomFactionDesign.MaxDesigns)
        {
            var r = c.NewRoster(Bases[faction]);
            foreach (var row in rows.Skip(offset).Take(CustomFactionDesign.MaxDesigns))
            {
                var d = r.Designs.Count == 1 && !r.Designs[0].Parts["chassis"].StartsWith("stock-") ? r.Designs[0] : c.AddDesign(r);
                c.SelectPart(c.Profile(r, d), "chassis", Hull(row));
            }
            var bytes = c.CompileRosterMap(r, File.ReadAllBytes(Path.Combine(DirectoryRoot, "lab.oramap")));
            using var zip = new ZipArchive(new MemoryStream(bytes));
            using var reader = new StreamReader(zip.GetEntry("modular-weapons.yaml").Open());
            Assert.That(reader.ReadToEnd(), Is.Empty);
            using var saved = new StreamReader(zip.GetEntry("custom-faction.json").Open());
            Assert.That(c.DeserializeRoster(saved.ReadToEnd()).Designs.Count, Is.EqualTo(Math.Min(16, rows.Length-offset)));
            var dir = Environment.GetEnvironmentVariable("MODULAR_STOCK_EXPORT_DIR");
            if (!string.IsNullOrEmpty(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                using var stream = new FileStream(Path.Combine(dir, faction + "-" + offset + ".oramap"), FileMode.CreateNew);
                stream.Write(bytes);
            }
        }
    }
}
