using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class DoubleEliminationTests
{
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
