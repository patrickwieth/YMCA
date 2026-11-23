using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class MatchResolutionChoicesTests
{
    static MatchRecord Match() => new() { Id = "M0023", PlayerOneDiscordId = 111, PlayerTwoDiscordId = 222,
        PlayerOneOpenRaName = "Alice", PlayerTwoOpenRaName = "Bob", Status = MatchStatus.Disputed };

    [Test]
    public void OnlyActualMatchSidesAreSuggestedAndUnknownChoicesAreRejected()
    {
        var match = Match();
        Assert.That(MatchResolutionChoices.Winners(match, "").Select(c => c.Name), Is.EqualTo(new[] { "Alice", "Bob" }));
        Assert.That(MatchResolutionChoices.Winners(match, "BO").Single().Value, Is.EqualTo("M0023:two"));
        Assert.That(MatchResolutionChoices.Winner(match, "M0023:one"), Is.EqualTo(111));
        Assert.Throws<InvalidOperationException>(() => MatchResolutionChoices.Winner(match, "YMCA Overlord"));
        Assert.Throws<InvalidOperationException>(() => MatchResolutionChoices.Winner(match, "M0022:one"));
        Assert.That(MatchResolutionChoices.Winners(null, ""), Is.Empty);
    }

    [Test]
    public void ExactNamesAreAcceptedOnlyWhenUniqueAndNeverOverrideStaleSelections()
    {
        var match = Match();
        Assert.That(MatchResolutionChoices.Winner(match, " alice "), Is.EqualTo(111));
        Assert.That(MatchResolutionChoices.Winner(match, "m0023:TWO"), Is.EqualTo(222));
        Assert.Throws<InvalidOperationException>(() => MatchResolutionChoices.Winner(match, "Ali"));
        match.PlayerOneTeamName = "Blue";
        match.PlayerOneTeammateOpenRaName = "Charlie";
        Assert.That(MatchResolutionChoices.Winner(match, "Charlie"), Is.EqualTo(111));
        Assert.That(MatchResolutionChoices.Winner(match, "blue"), Is.EqualTo(111));
        match.PlayerTwoOpenRaName = "Charlie";
        Assert.Throws<InvalidOperationException>(() => MatchResolutionChoices.Winner(match, "Charlie"));
        match.PlayerOneOpenRaName = "S0002:one";
        Assert.Throws<InvalidOperationException>(() => MatchResolutionChoices.Winner(match, "S0002:one"));
        Assert.That(MatchResolutionChoices.Winner(match, "M0023:one"), Is.EqualTo(111));
    }

    [Test]
    public void TeamsResolveToRepresentativesAndCompletedMatchesAreNotOffered()
    {
        var match = Match(); match.PlayerOneTeamName = "Blue"; match.PlayerOneTeammateOpenRaName = "Charlie";
        match.PlayerOneTeammateDiscordId = 333;
        Assert.That(MatchResolutionChoices.Winners(match, "Charlie").Single().Name, Is.EqualTo("Blue (Alice + Charlie)"));
        Assert.That(MatchResolutionChoices.Winner(match, "M0023:one"), Is.EqualTo(match.PlayerOneDiscordId));
        match.Status = MatchStatus.Completed;
        Assert.That(MatchResolutionChoices.Winners(match, ""), Is.Empty);
        match.Status = MatchStatus.Cancelled;
        Assert.That(MatchResolutionChoices.Winners(match, ""), Is.Empty);
    }
}
