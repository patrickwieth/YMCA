using System.Text.Json;
using NUnit.Framework;
using Ymca.TournamentBot;
using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class AttendanceTimerTests
{
    string root = null!;
    BotConfiguration config = null!;
    StateStore store = null!;
    OpenRaServerPool pool = null!;
    TournamentCoordinator coordinator = null!;
    TournamentRecord tournament = null!;

    [SetUp]
    public async Task Setup()
    {
        root = Path.Combine(Path.GetTempPath(), "ymca-attendance", Guid.NewGuid().ToString("N"));
        config = new BotConfiguration { StateFile = Path.Combine(root, "state.json"), Server = new OpenRaServerConfiguration { MatchDirectory = Path.Combine(root, "matches") } };
        store = new StateStore(config.StateFile);
        pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        coordinator = new TournamentCoordinator(config, store, pool);
        await coordinator.AddMapAsync("fixed-map", "Map", 4);
        tournament = await coordinator.CreateTournamentAsync("Cup", TournamentFormat.SingleElimination);
        for (ulong i = 1; i <= 4; i++) { await coordinator.RegisterAsync(i, "P" + i, "P" + i); await coordinator.JoinTournamentAsync(tournament.Id, i); }
        await coordinator.StartTournamentAsync(tournament.Id);
        await store.UpdateAsync(state =>
        {
            foreach (var match in state.Matches.Values)
            {
                match.Status = MatchStatus.WaitingForPlayers;
                match.SupportDirectory = Path.Combine(root, match.Id); Directory.CreateDirectory(match.SupportDirectory);
            }
        });
    }

    [TearDown]
    public async Task Cleanup() { await pool.DisposeAsync(); Directory.Delete(root, true); }

    [TestCase("", "Review")]
    [TestCase("A", "OneWins")]
    [TestCase("B", "TwoWins")]
    [TestCase("A,B", "Review")]
    public void PresenceDoesNotRequireReadyAndBothOrNeitherNeverGetsAnAutomaticWinner(string present, string outcome)
    {
        Assert.That(TournamentAttendanceProtocol.Decide(new[] { "A" }, new[] { "B" }, present.Split(',')), Is.EqualTo(outcome));
    }

    [TestCase("A", "Review")]
    [TestCase("A,C", "OneWins")]
    [TestCase("B,D", "TwoWins")]
    [TestCase("A,C,B", "Review")]
    [TestCase("A,C,B,D", "Review")]
    public void PartialTeamsRequireAdminReview(string present, string outcome)
    {
        Assert.That(TournamentAttendanceProtocol.Decide(new[] { "A", "C" }, new[] { "B", "D" }, present.Split(',')), Is.EqualTo(outcome));
    }

    [Test]
    public async Task OnlyExplicitTriggerArmsCurrentOpenGamesForExactlyFifteenMinutes()
    {
        await coordinator.CheckAttendanceTimersAsync();
        var matches = await coordinator.GetRecentMatchesAsync();
        Assert.That(matches.All(m => m.AttendanceTimer == null), Is.True);
        var before = DateTime.UtcNow;
        var armed = await coordinator.StartAttendanceTimerAsync(tournament.Id, 999);
        Assert.That(armed.Count, Is.EqualTo(2));
        Assert.That(armed.All(m => m.AttendanceTimer!.DeadlineUtc - m.AttendanceTimer.StartedAtUtc == TimeSpan.FromMinutes(15)), Is.True);
        Assert.That(armed.All(m => m.AttendanceTimer!.StartedByDiscordId == 999 && m.AttendanceTimer.StartedAtUtc >= before), Is.True);
        Assert.That(armed.All(m => File.Exists(Path.Combine(m.SupportDirectory, TournamentAttendanceProtocol.RequestFile))), Is.True);
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartAttendanceTimerAsync(tournament.Id, 999));
        var loaded = new StateStore(config.StateFile); await loaded.LoadAsync();
        Assert.That(await loaded.ReadAsync(s => s.Matches[armed[0].Id].AttendanceTimer!.DeadlineUtc), Is.EqualTo(armed[0].AttendanceTimer!.DeadlineUtc));
    }

    async Task<MatchRecord> ArmPastDeadline(string outcome)
    {
        var match = (await coordinator.StartAttendanceTimerAsync(tournament.Id, 999))[0];
        await store.UpdateAsync(_ => { match.AttendanceTimer!.DeadlineUtc = DateTime.UtcNow.AddSeconds(-2); match.AttendanceTimer.StartedAtUtc = match.AttendanceTimer.DeadlineUtc.AddMinutes(-15); });
        var decision = new TournamentAttendanceDecision { Id = match.AttendanceTimer!.Id, MatchId = match.Id, MapUid = match.MapUid,
            DecidedAtUtc = DateTime.UtcNow, Outcome = outcome };
        await File.WriteAllTextAsync(Path.Combine(match.SupportDirectory, TournamentAttendanceProtocol.DecisionFile), JsonSerializer.Serialize(decision));
        return match;
    }

    [Test]
    public async Task AuthoritativeForfeitAdvancesExactlyOnceAndDoesNotArmFutureGames()
    {
        var match = await ArmPastDeadline("OneWins");
        await coordinator.CheckAttendanceTimersAsync(); await coordinator.CheckAttendanceTimersAsync();
        Assert.That(match.Status, Is.EqualTo(MatchStatus.Completed));
        Assert.That(match.FinalWinnerDiscordId, Is.EqualTo(match.PlayerOneDiscordId));
        Assert.That(match.ForfeitReason, Does.Contain("admin-triggered"));
        var series = (await coordinator.GetSeriesAsync(match.SeriesId))!;
        Assert.That(series.Wins[match.PlayerOneDiscordId], Is.EqualTo(1));
        Assert.That(tournament.ProcessedMatchIds.Count, Is.EqualTo(1));
        var other = (await coordinator.GetRecentMatchesAsync()).Single(m => m.Id != match.Id);
        await coordinator.ResolveAsync(other.Id, other.PlayerOneDiscordId);
        Assert.That((await coordinator.GetRecentMatchesAsync(100)).Where(m => m.TournamentRound == 2).All(m => m.AttendanceTimer == null), Is.True);
    }

    [TestCase("Review")]
    [TestCase("Started")]
    public async Task NonForfeitDecisionsNeverChangeMatchResults(string outcome)
    {
        var match = await ArmPastDeadline(outcome);
        await coordinator.CheckAttendanceTimersAsync();
        Assert.That(match.Status, Is.EqualTo(MatchStatus.WaitingForPlayers));
        Assert.That(match.FinalWinnerDiscordId, Is.Null);
        Assert.That(match.AttendanceTimer!.Outcome, Is.Not.EqualTo("Pending"));
    }

    [Test]
    public async Task AlreadyStartedGamesAndManualResolutionsAreNeverOverridden()
    {
        var match = await ArmPastDeadline("OneWins");
        await store.UpdateAsync(_ => match.Status = MatchStatus.Playing);
        await coordinator.CheckAttendanceTimersAsync();
        Assert.That(match.Status, Is.EqualTo(MatchStatus.Playing));
        Assert.That(match.FinalWinnerDiscordId, Is.Null);
        await coordinator.ResolveAsync(match.Id, match.PlayerTwoDiscordId);
        await coordinator.CheckAttendanceTimersAsync();
        Assert.That(match.FinalWinnerDiscordId, Is.EqualTo(match.PlayerTwoDiscordId));
    }

    [Test]
    public async Task RestartRetainsDeadlineButNeverPenalizesPlayersForHostOutage()
    {
        var armed = await coordinator.StartAttendanceTimerAsync(tournament.Id, 999);
        var deadline = armed[0].AttendanceTimer!.DeadlineUtc;
        config.Server.MaxConcurrentServers = 0; // No external processes in this state-recovery test.
        await using var restartedPool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        var loaded = new StateStore(config.StateFile);
        var restarted = new TournamentCoordinator(config, loaded, restartedPool);
        await restarted.StartAsync();
        var match = (await restarted.GetMatchAsync(armed[0].Id))!;
        Assert.That(match.AttendanceTimer!.DeadlineUtc, Is.EqualTo(deadline));
        Assert.That(match.AttendanceTimer.EligibleForForfeit, Is.False);
        await restarted.CheckAttendanceTimersAsync(deadline.AddSeconds(40));
        Assert.That(match.AttendanceTimer.Outcome, Is.EqualTo("Review"));
        Assert.That(match.FinalWinnerDiscordId, Is.Null);
    }

    [Test]
    public async Task CrashBetweenSavedForfeitAndAdvancementIsRecoveredWithoutDoubleScoring()
    {
        var match = await ArmPastDeadline("OneWins");
        await store.UpdateAsync(_ => { match.Status = MatchStatus.Completed; match.FinalWinnerDiscordId = match.PlayerOneDiscordId; match.AttendanceTimer!.Outcome = "OneWins"; });
        config.Server.MaxConcurrentServers = 0;
        await using var restartedPool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        var loaded = new StateStore(config.StateFile);
        var restarted = new TournamentCoordinator(config, loaded, restartedPool);
        await restarted.StartAsync();
        var series = (await restarted.GetSeriesAsync(match.SeriesId))!;
        Assert.That(series.Wins[match.PlayerOneDiscordId], Is.EqualTo(1));
        Assert.That((await restarted.GetTournamentAsync(tournament.Id))!.ProcessedMatchIds, Does.Contain(match.Id));
        await restarted.CheckAttendanceTimersAsync(DateTime.UtcNow.AddMinutes(16));
        Assert.That(series.Wins[match.PlayerOneDiscordId], Is.EqualTo(1));
    }

    [Test]
    public async Task WarningIsPersistedAndPlayingGamesAreExcludedFromTrigger()
    {
        var playing = (await coordinator.GetRecentMatchesAsync())[0];
        await store.UpdateAsync(_ => playing.Status = MatchStatus.Playing);
        var armed = await coordinator.StartAttendanceTimerAsync(tournament.Id, 999);
        Assert.That(armed.Count, Is.EqualTo(1));
        Assert.That(playing.AttendanceTimer, Is.Null);
        await coordinator.CheckAttendanceTimersAsync(armed[0].AttendanceTimer!.DeadlineUtc.AddMinutes(-1));
        Assert.That(armed[0].AttendanceTimer!.WarningSent, Is.True);
    }

    [Test]
    public async Task MissingOrStaleServerDecisionAndUnavailableLobbyNeverForfeit()
    {
        var first = (await coordinator.GetRecentMatchesAsync())[0];
        await store.UpdateAsync(_ => first.Status = MatchStatus.Queued);
        var matches = await coordinator.StartAttendanceTimerAsync(tournament.Id, 999);
        Assert.That(first.AttendanceTimer!.EligibleForForfeit, Is.False);
        foreach (var match in matches)
        {
            var wrong = new TournamentAttendanceDecision { Id = Guid.NewGuid().ToString("N"), MatchId = match.Id, MapUid = match.MapUid, Outcome = "OneWins" };
            await File.WriteAllTextAsync(Path.Combine(match.SupportDirectory, TournamentAttendanceProtocol.DecisionFile), JsonSerializer.Serialize(wrong));
        }
        await coordinator.CheckAttendanceTimersAsync(DateTime.UtcNow.AddMinutes(16));
        Assert.That(matches.All(m => m.FinalWinnerDiscordId == null && m.AttendanceTimer!.Outcome == "Review"), Is.True);
    }
}
