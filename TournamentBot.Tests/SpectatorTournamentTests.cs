using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class SpectatorTournamentTests
{
    [Test]
    public async Task SpectatorSettingIsCopiedToScheduledMatches()
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
            await coordinator.AddMapAsync("map", "Map");
            var tournament = await coordinator.CreateTournamentAsync(
                "Public cup", TournamentFormat.SingleElimination, TournamentMode.OneVsOne, allowSpectators: true);

            for (ulong id = 1; id <= 2; id++)
            {
                await coordinator.RegisterAsync(id, $"Player {id}", $"Player{id}");
                await coordinator.JoinTournamentAsync(tournament.Id, id);
            }

            await coordinator.StartTournamentAsync(tournament.Id);
            var match = (await coordinator.GetRecentMatchesAsync()).Single();
            var started = await coordinator.GetTournamentAsync(tournament.Id);

            Assert.Multiple(() =>
            {
                Assert.That(started!.AllowSpectators, Is.True);
                Assert.That(match.AllowSpectators, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
