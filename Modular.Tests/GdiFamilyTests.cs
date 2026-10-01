using System.IO.Compression;
using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class GdiFamilyTests
{
    static CustomFactionDesign Compiler => new(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
    static CustomFactionProfile Design(CustomFactionDesign c, string hull)
    {
        var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", hull); return p;
    }

    [TestCase("juggernaut", 2000, 60000, 50, "Juggernaut")]
    [TestCase("designer-mammoth-hull", 1700, 78000, 52, "Mammoth")]
    [TestCase("designer-hmlrs-hull", 1150, 18000, 113, "hmlrs")]
    [TestCase("designer-disruptor-hull", 1500, 75000, 56, "DISR")]
    [TestCase("designer-mk2-hull", 10000, 350000, 35, "MAMMOTHMK2")]
    public void DefaultsMatchFullActorBaseline(string hull, int price, int hp, int speed, string actor)
    {
        var c = Compiler; var p = Design(c, hull); var v = c.Calculate(p);
        Assert.That(v.Cost, Is.EqualTo(price)); Assert.That(v.Hp, Is.EqualTo(hp)); Assert.That(v.Speed, Is.EqualTo(speed));
        Assert.That(c.Rules(p), Does.Contain("Inherits: " + actor + "\n"));
        Assert.That(c.CompatibleOptions(p, "running_gear"), Does.Not.Contain("gdi-stationary"));
        Assert.That(c.Weapons(p), Does.Not.Contain("SpreadDamage"), "Complex weapon/helper payloads must not be rewritten into generic damage warheads.");
    }

    [Test]
    public void JuggernautRetainsDummyAimAndDoesNotAcquireATurretOrDelayOnHelper()
    {
        var c = Compiler; var p = Design(c, "juggernaut");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: JuggernautDummyAim"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: JuggernautGun\n"));
        Assert.That(c.Rules(p), Does.Not.Contain("Turreted"));
        Assert.That(c.Rules(p), Does.Not.Contain("FireDelay:"));
        Assert.That(c.Rules(p), Does.Contain("Armament@SECONDARY:\n\t\tWeapon: modular.custom.w1"));
    }

    [Test]
    public void DisruptorPreservesVisualAndUpgradeChannelsWithoutNameCollisions()
    {
        var c = Compiler; var p = Design(c, "designer-disruptor-hull");
        var rules = c.Rules(p); var weapons = c.Weapons(p);
        foreach (var parent in new[] { "SonicZap", "SonicZapVisual", "SonicZap.UPG", "SonicZapVisual.UPG" })
            Assert.That(weapons, Does.Contain("Inherits: " + parent + "\n"));
        foreach (var slot in Enumerable.Range(0, 4))
        {
            Assert.That(weapons, Does.Contain("modular.custom.w" + slot + ":\n"));
            Assert.That(rules, Does.Contain("Weapon: modular.custom.w" + slot + "\n"));
        }
        Assert.That(rules, Does.Not.Contain("RequiresCondition:"), "Do not overwrite inherited gdiupg2 channel conditions.");
    }

    [Test]
    public void NativeHoverKeepsRealArtworkAndAllConditionalMissiles()
    {
        var c = Compiler; var p = Design(c, "designer-hmlrs-hull");
        Assert.That(c.IsNativeHover(p), Is.True);
        Assert.That(c.Rules(p), Does.Contain("Image: hmlrs"));
        Assert.That(c.Rules(p), Does.Not.Contain("Inherits@MODULARHOVER"), "Do not add a second hover trait group.");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 227mmH\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 227mmAAH\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 227mm.upg\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: 227mmAA.upg\n"));
        Assert.That(c.Rules(p), Does.Contain("~promotion.hover_mlrs"));
    }

    [Test]
    public void Mk2KeepsThreeWeaponsVoxelImageAndOriginalUnlock()
    {
        var c = Compiler; var p = Design(c, "designer-mk2-hull");
        Assert.That(c.Rules(p), Does.Contain("RenderVoxels:\n\t\tImage: mammothmk2"));
        Assert.That(c.Rules(p), Does.Not.Contain("Turreted"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tCarryable:"));
        Assert.That(c.Rules(p), Does.Contain("~promotion.mammoth_mkii, miss.gdi"));
        foreach (var parent in new[] { "Railgun.MKII", "Dragon.MKII", "RedEye.MKII" })
            Assert.That(c.Weapons(p), Does.Contain("Inherits: " + parent + "\n"));
    }

    [Test]
    public void AllEightFamiliesFitTogetherAndFreezeInOneMap()
    {
        var c = Compiler; var r = new CustomFactionRoster();
        foreach (var hull in c.CompatibleOptions(new CustomFactionProfile(), "chassis").Skip(1))
        {
            var d = c.AddDesign(r); c.SelectPart(c.Profile(r, d), "chassis", hull);
        }
        Assert.That(r.Designs.Count, Is.EqualTo(8));
        Assert.That(c.ValidateRoster(r), Is.LessThanOrEqualTo(50));
        var lab = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "lab.oramap"));
        var bytes = c.CompileRosterMap(r, lab);
        Assert.That(c.CompileRosterMap(r, lab), Is.EqualTo(bytes));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var reader = new StreamReader(zip.GetEntry("custom-faction.json").Open());
        Assert.That(c.DeserializeRoster(reader.ReadToEnd()).Designs.Count, Is.EqualTo(8));
        var path = Environment.GetEnvironmentVariable("MODULAR_GDI_EXPORT");
        if (!string.IsNullOrEmpty(path))
        {
            using var file = new FileStream(path, FileMode.CreateNew); file.Write(bytes);
        }
    }
}
