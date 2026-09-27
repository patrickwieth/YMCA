using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomFactionLobbySessionTests
{
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void OnlyDesignerSkirmishesBypassStockPersistence(bool skirmish, bool frozen, bool stock)
    {
        var session = new CustomFactionLobbySession();
        Assert.That(session.UseSavedSettings(skirmish, frozen), Is.EqualTo(stock));
    }

    [Test]
    public void ChangingMapInTestLobbyDoesNotOverwriteOrdinarySkirmishSetup()
    {
        var session = new CustomFactionLobbySession();
        Assert.That(session.UseSavedSettings(true, true), Is.False);
        Assert.That(session.UseSavedSettings(true, false), Is.False);
    }

    [Test]
    public void OrdinaryLobbyRetainsStockBehaviorAfterManualMapSelection()
    {
        var session = new CustomFactionLobbySession();
        Assert.That(session.UseSavedSettings(true, false), Is.True);
        Assert.That(session.UseSavedSettings(true, true), Is.True);
    }
}
