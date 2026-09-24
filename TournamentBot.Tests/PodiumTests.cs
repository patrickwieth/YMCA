using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class PodiumTests
{
    [Test]
    public async Task SingleEliminationSchedulesThirdPlacePlayoffAndRecordsPodium()
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
            var tournament = await coordinator.CreateTournamentAsync("Podium test", TournamentFormat.SingleElimination);

            for (ulong player = 1; player <= 4; player++)
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

                Assert.That(unresolved, Is.Not.Empty);
                foreach (var match in unresolved)
                    await coordinator.ResolveAsync(match.Id, match.PlayerOneDiscordId);
            }

            var completed = (await coordinator.GetTournamentAsync(tournament.Id))!;
            var matches = await coordinator.GetRecentMatchesAsync(10);
            Assert.Multiple(() =>
            {
                Assert.That(matches, Has.Count.EqualTo(4));
                Assert.That(matches.Count(match => match.IsThirdPlaceMatch), Is.EqualTo(1));
                Assert.That(completed.ChampionDiscordId, Is.Not.Null);
                Assert.That(completed.RunnerUpDiscordId, Is.Not.Null);
                Assert.That(completed.ThirdPlaceDiscordId, Is.Not.Null);
                Assert.That(new[]
                {
                    completed.ChampionDiscordId,
                    completed.RunnerUpDiscordId,
                    completed.ThirdPlaceDiscordId
                }.Distinct().Count(), Is.EqualTo(3));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
