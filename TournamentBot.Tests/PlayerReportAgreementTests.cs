using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class PlayerReportAgreementTests
{
    [Test]
    public async Task AgreedPlayerReportsOverrideConflictingAutomaticResult()
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
            await coordinator.RegisterAsync(1, "Player One", "PlayerOne");
            await coordinator.RegisterAsync(2, "Player Two", "PlayerTwo");
            var match = await coordinator.CreateMatchAsync(1, 2, "map", "Map");

            await store.UpdateAsync(state =>
            {
                var stored = state.Matches[match.Id];
                stored.AutomaticWinnerDiscordId = 2;
                stored.Status = MatchStatus.AwaitingConfirmation;
            });

            await coordinator.SubmitReportAsync(match.Id, 1, PlayerReport.Won);
            await coordinator.SubmitReportAsync(match.Id, 2, PlayerReport.Lost);

            var completed = (await coordinator.GetRecentMatchesAsync()).Single(value => value.Id == match.Id);
            Assert.Multiple(() =>
            {
                Assert.That(completed.Status, Is.EqualTo(MatchStatus.Completed));
                Assert.That(completed.FinalWinnerDiscordId, Is.EqualTo(1));
                Assert.That(completed.AutomaticWinnerDiscordId, Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
