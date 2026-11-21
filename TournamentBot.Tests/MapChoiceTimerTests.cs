using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class MapChoiceTimerTests
{
    string root = null!;
    BotConfiguration config = null!;
    StateStore store = null!;
    OpenRaServerPool pool = null!;
    TournamentCoordinator coordinator = null!;

    [SetUp]
    public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), "ymca-map-timer-" + Guid.NewGuid().ToString("N"));
        config = new BotConfiguration { StateFile = Path.Combine(root, "state.json"),
            Server = new OpenRaServerConfiguration { GenerateTournamentMaps = true, MatchDirectory = Path.Combine(root,"matches") } };
        store = new StateStore(config.StateFile);
        pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        coordinator = new TournamentCoordinator(config, store, pool);
    }

    [TearDown]
    public async Task Cleanup()
    {
        await pool.DisposeAsync();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    async Task<TournamentRecord> Create(TournamentFormat format = TournamentFormat.SingleElimination, int players = 4, int wins = 2)
    {
        var t = await coordinator.CreateTournamentAsync("Map timer cup",format,winsRequired:wins);
        for (ulong p=1;p<=(ulong)players;p++)
        {
            await coordinator.RegisterAsync(p,"P"+p,"P"+p);
            await coordinator.JoinTournamentAsync(t.Id,p);
        }
        await coordinator.StartTournamentAsync(t.Id);
        return t;
    }

    async Task<TournamentSeries> First(TournamentRecord t) => (await coordinator.GetSeriesAsync(t.SeriesIds[0]))!;
    Task Pick(TournamentSeries s, ulong p) => coordinator.SubmitSeriesMapPickAsync(s.Id,p,"__random__");

    [Test]
    public async Task ExplicitTimerIncludesMapChoicesEvenWithoutAnyGameServer()
    {
        var t=await Create();
        Assert.That(await coordinator.GetRecentMatchesAsync(),Is.Empty);
        await coordinator.CheckAttendanceTimersAsync(DateTime.UtcNow.AddHours(1));
        Assert.That((await First(t)).MapChoiceTimer,Is.Null);
        var started=await coordinator.StartTournamentTimerAsync(t.Id,999);
        Assert.That(started.Matches,Is.Empty);
        Assert.That(started.Series,Has.Count.EqualTo(2));
        Assert.That(started.Series.All(s=>s.MapChoiceTimer!.DeadlineUtc-s.MapChoiceTimer.StartedAtUtc==TimeSpan.FromMinutes(15)),Is.True);
        Assert.That(started.Series.All(s=>s.MapChoiceTimer!.StartedByDiscordId==999),Is.True);
        var deadline=started.DeadlineUtc;
        Assert.ThrowsAsync<InvalidOperationException>(()=>coordinator.StartTournamentTimerAsync(t.Id,999));
        Assert.That(started.Series[0].MapChoiceTimer!.DeadlineUtc,Is.EqualTo(deadline));
        var reload=new StateStore(config.StateFile);await reload.LoadAsync();
        Assert.That(await reload.ReadAsync(s=>s.Series[started.Series[0].Id].MapChoiceTimer!.DeadlineUtc),Is.EqualTo(deadline));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task MissingChoiceForfeitsEntireBo3ExactlyOnceWithoutFictitiousGames(bool firstWins)
    {
        var t=await Create();var s=await First(t);
        var winner=firstWins?s.PlayerOneDiscordId:s.PlayerTwoDiscordId;
        var loser=firstWins?s.PlayerTwoDiscordId:s.PlayerOneDiscordId;
        await Pick(s,winner);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc.AddTicks(-1));
        Assert.That(s.Status,Is.EqualTo(TournamentSeriesStatus.AwaitingMapPicks));
        await Task.WhenAll(coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc),coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc));
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc.AddMinutes(1));
        Assert.Multiple(()=>
        {
            Assert.That(s.Status,Is.EqualTo(TournamentSeriesStatus.Completed));
            Assert.That(s.Wins[winner],Is.EqualTo(2));
            Assert.That(s.ForfeitWinnerDiscordId,Is.EqualTo(winner));
            Assert.That(s.ForfeitReason,Does.Contain("entire series"));
            Assert.That(t.Losses[loser],Is.EqualTo(1));
            Assert.That(t.ProcessedSeriesIds.Count(id=>id==s.Id),Is.EqualTo(1));
            Assert.That(s.MatchIds,Is.Empty);
        });
        Assert.That(await coordinator.GetRecentMatchesAsync(),Is.Empty);
        Assert.ThrowsAsync<InvalidOperationException>(()=>Pick(s,loser));
    }

    [Test]
    public async Task BothMissingRequiresReviewAndExplicitRearmInsteadOfAnArbitraryWinner()
    {
        var t=await Create();var s=await First(t);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc);
        Assert.That(s.MapChoiceTimer!.Outcome,Is.EqualTo("Review"));
        Assert.That(s.ForfeitWinnerDiscordId,Is.Null);
        Assert.That(t.Losses.Values.All(l=>l==0),Is.True);
        Assert.That(s.Status,Is.EqualTo(TournamentSeriesStatus.AwaitingMapPicks));
        var oldId=s.MapChoiceTimer.Id;
        await coordinator.StartTournamentTimerAsync(t.Id,999);
        Assert.That(s.MapChoiceTimer.Id,Is.Not.EqualTo(oldId));
        Assert.That(s.MapChoiceTimer.Outcome,Is.EqualTo("Pending"));
    }

    [Test]
    public async Task TimelyChoicesCancelMapTimerAndDoNotArmTheNewGameOrSecondGame()
    {
        var t=await Create();var s=await First(t);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await Pick(s,s.PlayerOneDiscordId);await Pick(s,s.PlayerTwoDiscordId);
        Assert.That(s.MapChoiceTimer!.Outcome,Is.EqualTo("Completed"));
        var game=(await coordinator.GetMatchAsync(s.MatchIds[0]))!;
        Assert.That(game.AttendanceTimer,Is.Null);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc.AddMinutes(1));
        Assert.That(s.Status,Is.EqualTo(TournamentSeriesStatus.Playing));
        Assert.That(s.ForfeitWinnerDiscordId,Is.Null);
        await coordinator.ResolveAsync(game.Id,s.PlayerOneDiscordId);
        Assert.That(s.MatchIds,Has.Count.EqualTo(2));
        Assert.That((await coordinator.GetMatchAsync(s.MatchIds[1]))!.AttendanceTimer,Is.Null);
    }

    [Test]
    public async Task LateChoiceCannotBeatTheWatchersNextTick()
    {
        var t=await Create();var s=await First(t);
        await Pick(s,s.PlayerOneDiscordId);
        await coordinator.StartTournamentTimerAsync(t.Id,999);
        await store.UpdateAsync(_=>s.MapChoiceTimer!.DeadlineUtc=DateTime.UtcNow.AddSeconds(-1));
        Assert.ThrowsAsync<InvalidOperationException>(()=>Pick(s,s.PlayerTwoDiscordId));
        Assert.That(s.MapPicks[s.PlayerTwoDiscordId],Is.Empty);
        await coordinator.CheckAttendanceTimersAsync();
        Assert.That(s.ForfeitWinnerDiscordId,Is.EqualTo(s.PlayerOneDiscordId));
    }

    [Test]
    public async Task Bo5RequiresAllChoicesAndForfeitsTheWholeSeriesNotOneGame()
    {
        var t=await Create(wins:3);var s=await First(t);
        await Pick(s,s.PlayerOneDiscordId);await Pick(s,s.PlayerOneDiscordId);
        await Pick(s,s.PlayerTwoDiscordId);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc);
        Assert.That(s.ForfeitWinnerDiscordId,Is.EqualTo(s.PlayerOneDiscordId));
        Assert.That(s.Wins[s.PlayerOneDiscordId],Is.EqualTo(3));
        Assert.That(t.Losses[s.PlayerTwoDiscordId],Is.EqualTo(1));
    }

    [Test]
    public async Task DoubleEliminationAppliesOneLossPerWalkoverAndFinishesAfterTheSecondLoss()
    {
        var t=await Create(TournamentFormat.DoubleElimination,2);var s=await First(t);
        var winner=s.PlayerOneDiscordId;var loser=s.PlayerTwoDiscordId;
        await Pick(s,winner);
        var first=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(first.DeadlineUtc);
        Assert.That(t.Status,Is.EqualTo(TournamentStatus.Running));
        Assert.That(t.Losses[loser],Is.EqualTo(1));
        var final=(await coordinator.GetSeriesAsync(t.SeriesIds.Last()))!;
        Assert.That(final.MapChoiceTimer,Is.Null);
        await Pick(final,winner);
        var second=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(second.DeadlineUtc);
        Assert.That(t.Status,Is.EqualTo(TournamentStatus.Completed));
        Assert.That(t.ChampionDiscordId,Is.EqualTo(winner));
        Assert.That(t.RunnerUpDiscordId,Is.EqualTo(loser));
        Assert.That(t.Losses[loser],Is.EqualTo(2));
    }

    [Test]
    public async Task SimultaneousWalkoversAdvanceRoundAndLeaveFutureTimersUnset()
    {
        var t=await Create();
        foreach(var sid in t.SeriesIds.ToArray())
        {
            var s=(await coordinator.GetSeriesAsync(sid))!;await Pick(s,s.PlayerOneDiscordId);
        }
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc);
        Assert.That(t.RoundNumber,Is.EqualTo(2));
        foreach(var sid in t.SeriesIds)
        {
            var s=(await coordinator.GetSeriesAsync(sid))!;
            if(s.TournamentRound==2)Assert.That(s.MapChoiceTimer,Is.Null);
        }
        Assert.That(t.ProcessedSeriesIds,Has.Count.EqualTo(2));
    }

    [Test]
    public async Task MixedTriggerIncludesWaitingGamesAndChoicesButExcludesPlayingGames()
    {
        var t=await Create(players:6);
        var first=(await coordinator.GetSeriesAsync(t.SeriesIds[0]))!;
        await Pick(first,first.PlayerOneDiscordId);await Pick(first,first.PlayerTwoDiscordId);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        Assert.That(start.Matches,Has.Count.EqualTo(1));
        Assert.That(start.Series,Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RestartPreservesDeadlineButNeverForfeitsForBotDowntime()
    {
        var t=await Create();var s=await First(t);
        await Pick(s,s.PlayerOneDiscordId);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        config.Server.MaxConcurrentServers=0;
        var loaded=new StateStore(config.StateFile);
        await using var restartedPool=new OpenRaServerPool(config.Server,new ReplayMetadataReader(config.Server));
        var restarted=new TournamentCoordinator(config,loaded,restartedPool);
        await restarted.StartAsync();
        await restarted.CheckAttendanceTimersAsync(start.DeadlineUtc);
        var restored=(await restarted.GetSeriesAsync(s.Id))!;
        Assert.That(restored.MapChoiceTimer!.DeadlineUtc,Is.EqualTo(start.DeadlineUtc));
        Assert.That(restored.MapChoiceTimer.Outcome,Is.EqualTo("Review"));
        Assert.That(restored.ForfeitWinnerDiscordId,Is.Null);
    }

    [Test]
    public async Task WarningIsPersistedAndCancelledTournamentsAreNeverScored()
    {
        var t=await Create();var s=await First(t);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc.AddMinutes(-1));
        Assert.That(s.MapChoiceTimer!.WarningSent,Is.True);
        await Pick(s,s.PlayerOneDiscordId);
        await store.UpdateAsync(_=>t.Status=TournamentStatus.Cancelled);
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc);
        Assert.That(s.ForfeitWinnerDiscordId,Is.Null);
        Assert.That(s.MapChoiceTimer.Outcome,Is.EqualTo("Skipped"));
    }

    [Test]
    public async Task WebPageShowsDeadlineAndExplicitWalkoverInsteadOfInventedReplays()
    {
        var t=await Create();var s=await First(t);
        await Pick(s,s.PlayerOneDiscordId);
        var start=await coordinator.StartTournamentTimerAsync(t.Id,999);
        var before=await store.ReadAsync(state=>TournamentWebPage.Tournament(state,t.Id));
        Assert.That(before,Does.Contain("Map-choice deadline"));
        await coordinator.CheckAttendanceTimersAsync(start.DeadlineUtc);
        var after=await store.ReadAsync(state=>TournamentWebPage.Tournament(state,t.Id));
        Assert.That(after,Does.Contain("Walkover:"));
        Assert.That(after,Does.Contain("wins the entire series"));
        Assert.That(after,Does.Not.Contain("/replay/"));
    }
}
