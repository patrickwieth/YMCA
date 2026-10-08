using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class DesignerTurretClassTests
{
    static string Catalog => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json"));

    [TestCase("gdi-battle-hull", "medium-cannon-mount", "Medium Tank Turret", "Battle Tank Cannon Turret")]
    [TestCase("stock-gdrn-hull", "mini-turret-mount", "Mini", "Mini Turret Mount")]
    [TestCase("designer-hmlrs-hull", "designer-hmlrs-mount", "Medium Turret", "Dual Missile Launcher")]
    [TestCase("designer-marv-hull", "designer-marv-mount", "Super Heavy", "Triple Ion Cannon")]
    [TestCase("designer-titan-hull", "designer-titan-mount", "Heavy Walker", "Heavy Walker Turret")]
    public void NativeTurretClassesAndLabelsAreExplicit(string hull, string mount, string kind, string label)
    {
        var c = new CustomFactionDesign(Catalog);
        var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", hull);
        Assert.That(p.Parts["carrier"], Is.EqualTo(mount));
        Assert.That(c.TurretClass(p), Is.EqualTo(kind));
        Assert.That(c.Label(mount), Is.EqualTo(label));
        Assert.Throws<ArgumentException>(() => c.SelectPart(p, "carrier", hull == "designer-marv-hull" ? "mini-turret-mount" : "designer-marv-mount"));
        var data = JObject.Parse(Catalog);
        data["components"][mount]["turret_class"] = "wrong-class";
        Assert.Throws<InvalidDataException>(() => new CustomFactionDesign(data.ToString()).Calculate(p));
    }

    [TestCase("stock-gdrn-hull", "mini-turret-mount", 300)]
    [TestCase("stock-vulc-hull", "dual-gatling-turret", 800)]
    public void KnownLegacyMountsMigrateWithoutChangingStockPackage(string hull, string mount, int cost)
    {
        var c = new CustomFactionDesign(Catalog);
        var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", hull);
        var old = JObject.Parse(c.Serialize(p));
        old["Parts"]["carrier"] = "integrated-mount";
        var restored = c.Deserialize(old.ToString());
        Assert.That(restored.Parts["carrier"], Is.EqualTo(mount));
        Assert.That(c.Calculate(restored).Cost, Is.EqualTo(cost));
        Assert.That(c.UsesStockArmaments(restored), Is.True);
        Assert.That(c.Weapons(restored), Is.Empty);
        Assert.That(c.Rules(restored), Does.Not.Contain("\tArmament"));
        c.SelectPart(p, "chassis", "stock-mdrn-hull");
        Assert.That(p.Parts["carrier"], Is.EqualTo("integrated-mount"));
    }
}
