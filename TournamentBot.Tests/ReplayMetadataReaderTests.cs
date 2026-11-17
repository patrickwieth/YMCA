using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class ReplayMetadataReaderTests
{
    [Test]
    public void ParsesOpenRaUtilityOutput()
    {
        const string metadata =
            "replay.orarep:\n" +
            "\tMod: ca\n" +
            "\tVersion: v0.96.13\n" +
            "\tMapTitle: Tournament Island\n" +
            "\tPlayers:\n" +
            "\t\t0:\n" +
            "\t\t\tName: Alice\n" +
            "\t\t\tIsHuman: True\n" +
            "\t\t\tOutcome: Won\n" +
            "\t\t1:\n" +
            "\t\t\tName: Bob\n" +
            "\t\t\tIsHuman: True\n" +
            "\t\t\tOutcome: Lost\n";

        var result = ReplayMetadataReader.Parse("replay.orarep", metadata);

        Assert.That(result.Version, Is.EqualTo("v0.96.13"));
        Assert.That(result.MapTitle, Is.EqualTo("Tournament Island"));
        Assert.That(result.Players, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Players[0], Is.EqualTo(new ReplayPlayerResult("Alice", "Won", true)));
            Assert.That(result.Players[1], Is.EqualTo(new ReplayPlayerResult("Bob", "Lost", true)));
        });
    }
}
