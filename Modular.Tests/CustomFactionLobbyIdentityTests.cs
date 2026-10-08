using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomFactionLobbyIdentityTests
{
    [TestCase("eagle")]
    [TestCase("blackh")]
    [TestCase("chinatnk")]
    [TestCase("england")]
    [TestCase("russia")]
    [TestCase("traveler")]
    public void DisplayUsesFrozenNameAndRetainsNativeFactionForFlags(string faction)
    {
        var identity = CustomFactionDesign.ReadLobbyIdentity(
            "{\"Schema\":2,\"Name\":\"My own faction\",\"BaseFaction\":\"" + faction + "\",\"Designs\":[]}");
        Assert.That(identity.Side, Is.EqualTo("Custom"));
        Assert.That(identity.Name, Is.EqualTo("My own faction"));
        Assert.That(identity.BaseFaction, Is.EqualTo(faction));
        Assert.That(identity.AppliesTo(faction), Is.True);
        Assert.That(identity.AppliesTo("random"), Is.False);
        Assert.That(identity.AppliesTo(null), Is.False);
    }

    [Test]
    public void LegacySingleDesignSnapshotIsSupportedAndNameIsNotLocalized()
    {
        var identity = CustomFactionDesign.ReadLobbyIdentity(
            "{\"Schema\":1,\"Name\":\"button-cancel\",\"BaseFaction\":\"eagle\",\"Parts\":{}}");
        Assert.That(identity.Name, Is.EqualTo("button-cancel"));
    }

    [TestCase("{}")]
    [TestCase("{\"Schema\":99,\"Name\":\"Name\",\"BaseFaction\":\"eagle\",\"Designs\":[]}")]
    [TestCase("{\"Schema\":2,\"Name\":\"   \",\"BaseFaction\":\"eagle\",\"Designs\":[]}")]
    [TestCase("{\"Schema\":2,\"Name\":\"Name\",\"BaseFaction\":\"Custom\",\"Designs\":[]}")]
    [TestCase("{\"Schema\":2,\"Name\":\"Name\",\"BaseFaction\":\"eagle\"}")]
    public void InvalidMetadataCannotReplaceStockLabels(string json)
        => Assert.Throws<InvalidDataException>(() => CustomFactionDesign.ReadLobbyIdentity(json));
}
