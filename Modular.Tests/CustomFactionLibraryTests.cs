using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomFactionLibraryTests
{
    string root;
    CustomFactionDesign compiler;
    CustomFactionLibrary library;
    [SetUp]
    public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        compiler = new CustomFactionDesign(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
        library = new CustomFactionLibrary(compiler, Path.Combine(root, "Factions"));
    }
    [TearDown]
    public void Cleanup() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    [Test]
    public void RenamePreservesIdentityAndBacksUpWithoutCreatingAnotherFaction()
    {
        var entry = library.Create("First", "eagle");
        var original = File.ReadAllText(entry.Path);
        entry.Roster.Name = "Renamed";
        library.Update(entry.Path, entry.Roster);
        Assert.That(library.Entries().Single().Path, Is.EqualTo(entry.Path));
        Assert.That(library.Load(entry.Path).Name, Is.EqualTo("Renamed"));
        Assert.That(File.ReadAllText(entry.Path + ".bak"), Is.EqualTo(original));
        var second = library.Create("Second", "eagle");
        second.Roster.Name = " renamed ";
        Assert.Throws<InvalidDataException>(() => library.Update(second.Path, second.Roster));
        Assert.That(library.Load(second.Path).Name, Is.EqualTo("Second"));
        Assert.Throws<InvalidDataException>(() => library.Create("RENAMED", "eagle"));
    }

    [Test]
    public void LegacyImportIsOneTimeAndDeletedFactionDoesNotResurrect()
    {
        Directory.CreateDirectory(root);
        var active = Path.Combine(root, "custom-faction.json");
        compiler.SaveRoster(active, new CustomFactionRoster());
        var snapshot = File.ReadAllText(active);
        library.ImportLegacyOnce(active);
        var entry = library.Entries().Single();
        library.Delete(entry.Path);
        library.ImportLegacyOnce(active);
        Assert.That(library.Entries(), Is.Empty);
        Assert.That(File.ReadAllText(active), Is.EqualTo(snapshot));
        Assert.That(Directory.GetFiles(Path.Combine(root, "Factions", "Deleted")).Length, Is.EqualTo(1));
        Assert.Throws<FileNotFoundException>(() => library.Update(entry.Path, entry.Roster));
    }

    [Test]
    public void InvalidPathsAndNamesCannotWriteOrDeleteOtherFiles()
    {
        var entry = library.Create("Valid", "eagle");
        var outside = Path.Combine(root, "faction-abcd.json");
        File.WriteAllText(outside, "keep");
        Assert.Throws<InvalidDataException>(() => library.Update(outside, entry.Roster));
        Assert.Throws<InvalidDataException>(() => library.Delete(outside));
        Assert.Throws<InvalidDataException>(() => library.Create("../escape", "eagle"));
        Assert.That(File.ReadAllText(outside), Is.EqualTo("keep"));
        File.WriteAllText(entry.Path, "broken");
        Assert.That(library.Entries().Single().Roster, Is.Null);
        library.Delete(entry.Path);
        Assert.That(library.Entries(), Is.Empty);
    }

    [Test]
    public void LevelDefaultsToFiftyAndControlsCatalogBudgetNotSlotCount()
    {
        var roster = new CustomFactionRoster();
        var json = JObject.Parse(compiler.SerializeRoster(roster));
        json.Remove("Level");
        Assert.That(compiler.DeserializeRoster(json.ToString()).Level, Is.EqualTo(50));
        var points = compiler.ValidateRoster(roster);
        Assert.That(points, Is.GreaterThan(0));
        roster.Level = points;
        Assert.That(compiler.ValidateRoster(roster), Is.EqualTo(points));
        Assert.Throws<InvalidDataException>(() => compiler.AddDesign(roster));
        Assert.That(roster.Designs.Count, Is.EqualTo(1));
        roster.Level = 100;
        Assert.That(compiler.DeserializeRoster(compiler.SerializeRoster(roster)).Level, Is.EqualTo(100));
        roster.Level = 101;
        Assert.Throws<InvalidDataException>(() => compiler.ValidateRoster(roster));
        roster.Level = 0;
        Assert.Throws<InvalidDataException>(() => compiler.ValidateRoster(roster));
    }
}
