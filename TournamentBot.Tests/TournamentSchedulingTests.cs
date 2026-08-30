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
            await coordinator.AddMapAsync("map-a", "Map A");
            await coordinator.AddMapAsync("map-b", "Map B");
            await coordinator.AddMapAsync("map-c", "Map C");
            var tournament = await coordinator.CreateTournamentAsync("Test", format);

            for (ulong player = 1; player <= (ulong)playerCount; player++)
            {
                await coordinator.RegisterAsync(player, $"Player {player}", $"Player{player}");
                await coordinator.JoinTournamentAsync(tournament.Id, player);
            }

            await coordinator.StartTournamentAsync(tournament.Id);
            string? previousRoundMap = null;
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
                Assert.That(unresolved.Select(match => match.MapUid).Distinct().ToList(), Has.Count.EqualTo(1),
                    "All matches in a round must use the same map.");
                var roundMap = unresolved[0].MapUid;
                if (previousRoundMap != null)
                    Assert.That(roundMap, Is.Not.EqualTo(previousRoundMap),
                        "Consecutive rounds must not repeat a map while alternatives exist.");
                previousRoundMap = roundMap;

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
