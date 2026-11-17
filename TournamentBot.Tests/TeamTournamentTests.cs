using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TeamTournamentTests
{
    [Test]
    public async Task TwoVsTwoRequiresAcceptedTeamsAndAdvancesByTeam()
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
            await coordinator.AddMapAsync("map-4p", "Four Players", 4);
            for (ulong id = 1; id <= 4; id++)
                await coordinator.RegisterAsync(id, $"Player {id}", $"Player{id}");

            var tournament = await coordinator.CreateTournamentAsync(
                "Team cup", TournamentFormat.SingleElimination, TournamentMode.TwoVsTwo);
            await coordinator.InviteTeamAsync(tournament.Id, 1, 2, "Alpha");
            await coordinator.RespondToTeamInviteAsync(tournament.Id, 1, 2, true);
            await coordinator.InviteTeamAsync(tournament.Id, 3, 4, "Bravo");
            await coordinator.RespondToTeamInviteAsync(tournament.Id, 3, 4, true);
            await coordinator.StartTournamentAsync(tournament.Id);

            var match = (await coordinator.GetRecentMatchesAsync()).Single();
            Assert.Multiple(() =>
            {
                Assert.That(match.PlayerOneTeammateDiscordId, Is.Not.Null);
                Assert.That(match.PlayerTwoTeammateDiscordId, Is.Not.Null);
                Assert.That(match.PlayerOneTeamName, Is.Not.Empty);
                Assert.That(match.PlayerTwoTeamName, Is.Not.Empty);
            });

            var winningTeammate = match.PlayerOneTeammateDiscordId!.Value;
            await coordinator.SubmitReportAsync(match.Id, winningTeammate, PlayerReport.Won);
            await coordinator.SubmitReportAsync(match.Id, match.PlayerTwoDiscordId, PlayerReport.Lost);

            var completed = await coordinator.GetTournamentAsync(tournament.Id);
            Assert.Multiple(() =>
            {
                Assert.That(completed!.Status, Is.EqualTo(TournamentStatus.Completed));
                Assert.That(completed.ChampionDiscordId, Is.EqualTo(match.PlayerOneDiscordId));
                Assert.That(completed.RunnerUpDiscordId, Is.EqualTo(match.PlayerTwoDiscordId));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
