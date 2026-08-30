using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentSchedulingTests
{
    [TestCase(TournamentFormat.SingleElimination, 5, 4, 1)]
    [TestCase(TournamentFormat.DoubleElimination, 8, 14, 2)]
    public async Task EliminatesPlayersAndCompletesWithByes(
        TournamentFormat format,
        int playerCount,
        int expectedMatches,
        int eliminationLosses)
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
            var tournament = await coordinator.CreateTournamentAsync("Test", format, "map", "Map");

            for (ulong player = 1; player <= (ulong)playerCount; player++)
            {
                await coordinator.RegisterAsync(player, $"Player {player}", $"Player{player}");
                await coordinator.JoinTournamentAsync(tournament.Id, player);
            }

            await coordinator.StartTournamentAsync(tournament.Id);
            while ((await coordinator.GetTournamentAsync(tournament.Id))!.Status == TournamentStatus.Running)
            {
                var current = (await coordinator.GetTournamentAsync(tournament.Id))!;
                var unresolved = new List<MatchRecord>();
                foreach (var matchId in current.MatchIds.Where(id => !current.ProcessedMatchIds.Contains(id)))
                {
                    var match = await coordinator.GetMatchAsync(matchId);
                    if (match!.Status == MatchStatus.Queued)
                        unresolved.Add(match);
                }

                Assert.That(unresolved, Is.Not.Empty, "A running tournament must have matches to resolve.");
                foreach (var match in unresolved)
                    await coordinator.ResolveAsync(match.Id, match.PlayerOneDiscordId);
            }

            var completed = (await coordinator.GetTournamentAsync(tournament.Id))!;
            Assert.Multiple(() =>
            {
                Assert.That(completed.MatchIds, Has.Count.EqualTo(expectedMatches));
                Assert.That(completed.ChampionDiscordId, Is.Not.Null);
                Assert.That(completed.Losses.Where(entry => entry.Key != completed.ChampionDiscordId).All(
                    entry => entry.Value == eliminationLosses), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
