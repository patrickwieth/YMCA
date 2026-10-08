using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomPreviewTests
{
    [Test]
    public void SlowRotationIsTimeBasedAndWrapsWithoutOverflow()
    {
        Assert.That(CustomPreviewMath.Facing(0), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.Facing(6000), Is.EqualTo(640));
        Assert.That(CustomPreviewMath.Facing(24000), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.Facing(-1), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.Facing(long.MaxValue), Is.InRange(0, 1023));
    }

    [Test]
    public void TurretCounterRotationUsesTheSameClockAndPeriod()
    {
        Assert.That(CustomPreviewMath.CounterFacing(0), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.CounterFacing(6000), Is.EqualTo(128));
        Assert.That(CustomPreviewMath.CounterFacing(24000), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.CounterFacing(-1), Is.EqualTo(384));
        Assert.That(CustomPreviewMath.CounterFacing(long.MaxValue), Is.InRange(0, 1023));
        for (var time = 0; time < 24000; time += 750)
            Assert.That((CustomPreviewMath.Facing(time) + CustomPreviewMath.CounterFacing(time)) % 1024, Is.EqualTo(768));
    }

    [Test]
    public void FitKeepsLargeModelsInsideThePanelAndDoesNotOvermagnifyTinySprites()
    {
        Assert.That(CustomPreviewMath.Fit(1000, 500, 216, 168), Is.EqualTo(.216f).Within(.0001));
        Assert.That(CustomPreviewMath.Fit(100, 1000, 216, 168), Is.EqualTo(.168f).Within(.0001));
        Assert.That(CustomPreviewMath.Fit(10, 10, 216, 168), Is.EqualTo(2));
        Assert.That(CustomPreviewMath.Fit(0, 10, 216, 168), Is.EqualTo(1));
        Assert.That(CustomPreviewMath.Fit(double.NaN, 10, 216, 168), Is.EqualTo(1));
    }

    [Test]
    public void PreviewSourceUsesBoundActorsAndRetainsNewGdiWeaponsUnlocksAndBehaviorParents()
    {
        var c = new CustomFactionDesign(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "designer-catalog.json")));
        var p = new CustomFactionProfile(); c.SelectPart(p, "chassis", "designer-titan-hull");
        Assert.That(c.PreviewActor(p), Is.EqualTo("TITN"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: TitanGun\n"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: TitanTusk\n"));
        Assert.That(c.Rules(p), Does.Not.Contain("\tTurreted@PRIMARY:"));
        Assert.That(c.Rules(p), Does.Contain("~!upg.titan"));
        c.SelectPart(p, "chassis", "designer-slingshot-hull");
        Assert.That(c.IsNativeHover(p), Is.True);
        Assert.That(c.Rules(p), Does.Contain("Locomotor: lighthover"));
        Assert.That(c.Rules(p), Does.Contain("~promotion.slingshot"));
        Assert.That(c.Weapons(p), Does.Contain("Inherits: SlingshotAA\n"));
        c.SelectPart(p, "chassis", "designer-marv-hull");
        Assert.That(c.Rules(p), Does.Contain("~promotion.marv, miss.gdi"));
        Assert.That(c.Rules(p), Does.Not.Contain("CashTrickler:"), "Keep full inherited harvesting, not a second income implementation.");
        Assert.That(c.Weapons(p), Does.Contain("Inherits: IonZap.Marv\n"));
    }
}
