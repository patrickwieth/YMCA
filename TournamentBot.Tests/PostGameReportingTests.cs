using System.Reflection;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class PostGameReportingTests
{
    [TestCase(MatchStatus.Playing, MatchStatus.AwaitingConfirmation)]
    [TestCase(MatchStatus.Completed, MatchStatus.Completed)]
    [TestCase(MatchStatus.Cancelled, MatchStatus.Cancelled)]
    [TestCase(MatchStatus.RematchRequested, MatchStatus.RematchRequested)]
    public async Task ResultIsRecordedImmediatelyWhileServerIsRetainedAndFinalDecisionsSurvive(MatchStatus before, MatchStatus after)
    {
        var root = Path.Combine(Path.GetTempPath(), "ymca-reporting", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var config = new BotConfiguration { StateFile = Path.Combine(root, "state.json") };
        var store = new StateStore(config.StateFile);
        await using var pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        var coordinator = new TournamentCoordinator(config, store, pool);
        var match = new MatchRecord { Id = "M1", Status = before, PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2,
            PlayerOneOpenRaName = "One", PlayerTwoOpenRaName = "Two", FinalWinnerDiscordId = before == MatchStatus.Completed ? 2UL : null };
        await store.UpdateAsync(state => state.Matches[match.Id] = match);
        var result = new ReplayResult("replay.orarep", "Map", "v0.97.4", new[] { new ReplayPlayerResult("One", "Won", true), new ReplayPlayerResult("Two", "Lost", true) });
        var started = DateTime.UtcNow;
        var callback = typeof(TournamentCoordinator).GetMethod("OnResultAvailableAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await ((Task)callback.Invoke(coordinator, new object[] { match, result })!).WaitAsync(TimeSpan.FromSeconds(2));
        var saved = (await coordinator.GetMatchAsync(match.Id))!;
        Assert.That(saved.Status, Is.EqualTo(after));
        Assert.That(saved.ReplayPath, Is.EqualTo("replay.orarep"));
        Assert.That(saved.ServerRetainUntilUtc, Is.GreaterThanOrEqualTo(started.AddMinutes(3)));
        if (before == MatchStatus.Completed) Assert.That(saved.FinalWinnerDiscordId, Is.EqualTo(2), "Do not undo an admin resolution");
        var stop = typeof(TournamentCoordinator).GetMethod("OnServerStoppedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)stop.Invoke(coordinator, new object[] { match })!;
        Assert.That(saved.ServerRetainUntilUtc, Is.Null);
        var failed = typeof(TournamentCoordinator).GetMethod("OnServerFailedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)failed.Invoke(coordinator, new object[] { match, "late host cleanup failure" })!;
        Assert.That(saved.Status, Is.EqualTo(after), "Host cleanup must not replace a recorded result");
        Directory.Delete(root, true);
    }
}
