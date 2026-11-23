using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class DoubleEliminationTests
{
    [TestCase(3, 1)]
    [TestCase(8, 1)]
    [TestCase(9, 1)]
    [TestCase(8, 0)]
    public async Task FixedSlotsSurviveCoordinatorRestartsAndGrandFinalReset(int count, int version)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-tournament-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var config = new BotConfiguration { StateFile = Path.Combine(directory, "state.json"),
                Server = new OpenRaServerConfiguration { MatchDirectory = Path.Combine(directory, "matches"), MaxConcurrentServers = 1 } };
            await using var pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
            var coordinator = new TournamentCoordinator(config, new StateStore(config.StateFile), pool);
            await coordinator.AddMapAsync("map-a", "Map A");
            var t = await coordinator.CreateTournamentAsync("Fixed bracket", TournamentFormat.DoubleElimination);
            for (ulong p = 1; p <= (ulong)count; p++)
            {
                await coordinator.RegisterAsync(p, $"Player{p}", $"Player{p}");
                await coordinator.JoinTournamentAsync(t.Id, p);
            }
            await coordinator.StartTournamentAsync(t.Id);
            if (version == 0)
            {
                // Simulate loading a tournament already running before fixed routing existed.
                var historical = new StateStore(config.StateFile);
                await historical.LoadAsync();
                await historical.UpdateAsync(state =>
                {
                    state.Tournaments[t.Id].BracketRoutingVersion = 0;
                    foreach (var s in state.Series.Values) s.BracketSlotId = "";
                });
                coordinator = new TournamentCoordinator(config, historical, pool);
            }
            var processed = new HashSet<string>();
            while ((t = (await coordinator.GetTournamentAsync(t.Id))!).Status == TournamentStatus.Running)
            {
                var pending = (await coordinator.GetRecentMatchesAsync()).Where(m => m.TournamentId == t.Id && !processed.Contains(m.Id)).ToArray();
                Assert.That(pending, Is.Not.Empty);
                foreach (var match in pending)
                {
                    var series = (await coordinator.GetSeriesAsync(match.SeriesId))!;
                    Assert.That(series.BracketSlotId.Length > 0, Is.EqualTo(version == 1));
                    var winner = match.PlayerOneDiscordId;
                    if (t.Losses.Values.Count(loss => loss < 2) == 2 &&
                        t.Losses[match.PlayerOneDiscordId] != t.Losses[match.PlayerTwoDiscordId])
                        winner = t.Losses[match.PlayerOneDiscordId] == 1 ? match.PlayerOneDiscordId : match.PlayerTwoDiscordId;
                    await coordinator.ResolveAsync(match.Id, winner);
                    processed.Add(match.Id);
                }
                var reloaded = new StateStore(config.StateFile);
                await reloaded.LoadAsync();
                coordinator = new TournamentCoordinator(config, reloaded, pool);
                Assert.That(processed.Count, Is.LessThanOrEqualTo(2 * count - 1));
            }
            Assert.That(t.BracketRoutingVersion, Is.EqualTo(version));
            Assert.That(t.MatchIds.Count, Is.EqualTo(2 * count - 1));
            Assert.That(t.Losses.Values.Count(loss => loss == 2), Is.EqualTo(count - 1));
            Assert.That(t.ThirdPlaceDiscordId, Is.Not.Null);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public async Task LosersBracketWinnerMustWinGrandFinalTwice()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-tournament-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var config = new BotConfiguration
            {
                StateFile = Path.Combine(directory, "state.json"),
                Server = new OpenRaServerConfiguration
                {
                    MatchDirectory = Path.Combine(directory, "matches"),
                    MaxConcurrentServers = 1
                }
            };
            var store = new StateStore(config.StateFile);
            await using var pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
            var coordinator = new TournamentCoordinator(config, store, pool);

            const ulong alice = 1;
            const ulong bob = 2;
            await coordinator.RegisterAsync(alice, "Alice", "Alice");
            await coordinator.RegisterAsync(bob, "Bob", "Bob");
            await coordinator.AddMapAsync("map-a", "Map A");
            await coordinator.AddMapAsync("map-b", "Map B");
            var tournament = await coordinator.CreateTournamentAsync(
                "Double test", TournamentFormat.DoubleElimination);
            await coordinator.JoinTournamentAsync(tournament.Id, alice);
            await coordinator.JoinTournamentAsync(tournament.Id, bob);
            await coordinator.StartTournamentAsync(tournament.Id);

            var first = (await coordinator.GetRecentMatchesAsync()).Single();
            await coordinator.ResolveAsync(first.Id, alice);

            var second = (await coordinator.GetRecentMatchesAsync()).First(match => match.Id != first.Id);
            await coordinator.ResolveAsync(second.Id, bob);
            var afterReset = await coordinator.GetTournamentAsync(tournament.Id);
            Assert.That(afterReset!.Status, Is.EqualTo(TournamentStatus.Running));

            var final = (await coordinator.GetRecentMatchesAsync()).First(match =>
                match.Id != first.Id && match.Id != second.Id);
            await coordinator.ResolveAsync(final.Id, alice);

            var completed = await coordinator.GetTournamentAsync(tournament.Id);
            Assert.Multiple(() =>
            {
                Assert.That(completed!.Status, Is.EqualTo(TournamentStatus.Completed));
                Assert.That(completed.ChampionDiscordId, Is.EqualTo(alice));
                Assert.That(completed.RunnerUpDiscordId, Is.EqualTo(bob));
                Assert.That(completed.ThirdPlaceDiscordId, Is.Null);
                Assert.That(completed.Losses[bob], Is.EqualTo(2));
                Assert.That(completed.MatchIds, Has.Count.EqualTo(3));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
