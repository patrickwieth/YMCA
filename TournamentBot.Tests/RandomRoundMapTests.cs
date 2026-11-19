using System.Text.Json;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class RandomRoundMapTests
{
    string directory = null!;
    BotConfiguration config = null!;
    StateStore store = null!;
    OpenRaServerPool pool = null!;
    TournamentCoordinator coordinator = null!;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "ymca-round-maps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        config = new BotConfiguration { StateFile = Path.Combine(directory, "state.json"),
            Server = new OpenRaServerConfiguration { GenerateTournamentMaps = true, MatchDirectory = Path.Combine(directory, "matches") } };
        store = new StateStore(config.StateFile);
        pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        coordinator = new TournamentCoordinator(config, store, pool);
    }

    [TearDown]
    public async Task TearDown()
    {
        await pool.DisposeAsync();
        Directory.Delete(directory, true);
    }

    async Task<TournamentRecord> Create(int wins, bool generated = true,
        TournamentMode mode = TournamentMode.OneVsOne, TournamentFormat format = TournamentFormat.SingleElimination,
        TournamentMapSelection selection = TournamentMapSelection.RoundRandom)
    {
        config.Server.GenerateTournamentMaps = generated;
        if (!generated)
            for (var i = 0; i < 6; i++) await coordinator.AddMapAsync("map" + i, "Map " + i, 4);
        var tournament = await coordinator.CreateTournamentAsync("Round maps", format, mode, winsRequired: wins, mapSelection: selection);
        for (ulong i = 1; i <= (mode == TournamentMode.TwoVsTwo ? 8UL : 4UL); i++)
            await coordinator.RegisterAsync(i, "P" + i, "P" + i);
        if (mode == TournamentMode.TwoVsTwo)
            for (ulong i = 1; i <= 8; i += 2)
            {
                await coordinator.InviteTeamAsync(tournament.Id, i, i + 1, "Team" + i);
                await coordinator.RespondToTeamInviteAsync(tournament.Id, i, i + 1, true);
            }
        else
            for (ulong i = 1; i <= 4; i++) await coordinator.JoinTournamentAsync(tournament.Id, i);
        return tournament;
    }

    [Test]
    public async Task AllPairingsSharePersistedSequenceAndStartWithoutPicks(
        [Values(1, 2, 3)] int wins, [Values] bool generated,
        [Values] TournamentMode mode, [Values] TournamentFormat format)
    {
        var tournament = await Create(wins, generated, mode, format);
        tournament = await coordinator.StartTournamentAsync(tournament.Id);
        var sequence = tournament.MapsByRound[1];
        Assert.That(sequence.Count, Is.EqualTo(wins * 2 - 1));
        Assert.That(sequence.Select(m => m.Uid).Distinct().Count(), Is.EqualTo(sequence.Count));
        foreach (var id in tournament.SeriesIds)
        {
            var series = (await coordinator.GetSeriesAsync(id))!;
            Assert.That(series.Status, Is.EqualTo(TournamentSeriesStatus.Playing));
            Assert.That(JsonSerializer.Serialize(series.Maps), Is.EqualTo(JsonSerializer.Serialize(sequence)));
            Assert.That(series.MapPicks.Values.All(p => p.Count == 0), Is.True);
            Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SubmitSeriesMapPickAsync(id, series.PlayerOneDiscordId, "__random__"));
        }
        var matches = await coordinator.GetRecentMatchesAsync();
        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches.All(m => m.MapUid == sequence[0].Uid), Is.True);
        if (generated)
        {
            Assert.That(sequence.All(m => m.Generation!.Players == (mode == TournamentMode.TwoVsTwo ? 4 : 2)), Is.True);
            Assert.That(matches.All(m => m.Generation!.Id == sequence[0].Generation!.Id), Is.True);
            Assert.That(matches.All(m => m.Generation!.Seed == sequence[0].Generation!.Seed), Is.True);
        }
        var reloaded = new StateStore(config.StateFile); await reloaded.LoadAsync();
        var restored = await reloaded.ReadAsync(s => s.Tournaments[tournament.Id]);
        Assert.That(restored.MapSelection, Is.EqualTo(TournamentMapSelection.RoundRandom));
        Assert.That(JsonSerializer.Serialize(restored.MapsByRound), Is.EqualTo(JsonSerializer.Serialize(tournament.MapsByRound)));
    }

    [TestCase(TournamentFormat.SingleElimination)]
    [TestCase(TournamentFormat.DoubleElimination)]
    public async Task LaterRoundsFreezeNewSequencesAndPreservePreviousRounds(TournamentFormat format)
    {
        var tournament = await Create(2, format: format);
        await coordinator.StartTournamentAsync(tournament.Id);
        var first = JsonSerializer.Serialize(tournament.MapsByRound[1]);
        for (var i = 0; tournament.RoundNumber == 1 && i < 10; i++)
        {
            var match = (await coordinator.GetRecentMatchesAsync(100)).First(m => m.Status == MatchStatus.Queued);
            await coordinator.ResolveAsync(match.Id, match.PlayerOneDiscordId);
        }
        Assert.That(tournament.RoundNumber, Is.EqualTo(2));
        Assert.That(JsonSerializer.Serialize(tournament.MapsByRound[1]), Is.EqualTo(first));
        Assert.That(tournament.MapsByRound[2].Select(m => m.Generation!.Id)
            .Intersect(tournament.MapsByRound[1].Select(m => m.Generation!.Id)), Is.Empty);
        var roundTwoSeries = await store.ReadAsync(s => s.Series.Values.Where(v => v.TournamentRound == 2).ToList());
        foreach (var series in roundTwoSeries)
            Assert.That(JsonSerializer.Serialize(series.Maps), Is.EqualTo(JsonSerializer.Serialize(tournament.MapsByRound[2])));
        Assert.That((await coordinator.GetRecentMatchesAsync(100)).Count(m => m.TournamentRound == 1), Is.EqualTo(4), "No unused decider queued after 2-0");
    }

    [Test]
    public async Task RematchReusesExactRequestAndNextGameUsesNextSlot()
    {
        var tournament = await Create(3);
        await coordinator.StartTournamentAsync(tournament.Id);
        var first = (await coordinator.GetRecentMatchesAsync()).First();
        await coordinator.SubmitReportAsync(first.Id, first.PlayerOneDiscordId, PlayerReport.Rematch);
        await coordinator.SubmitReportAsync(first.Id, first.PlayerTwoDiscordId, PlayerReport.Rematch);
        var rematch = (await coordinator.GetRecentMatchesAsync()).Single(m => m.ParentMatchId == first.Id);
        Assert.That(rematch.Generation!.Id, Is.EqualTo(first.Generation!.Id));
        Assert.That(rematch.Generation.Seed, Is.EqualTo(first.Generation.Seed));
        await coordinator.ResolveAsync(rematch.Id, rematch.PlayerOneDiscordId);
        var next = (await coordinator.GetRecentMatchesAsync()).Single(m => m.SeriesId == first.SeriesId && m.SeriesGameNumber == 2);
        Assert.That(next.Generation!.Id, Is.EqualTo(tournament.MapsByRound[1][1].Generation!.Id));
    }

    [TestCase(1)]
    [TestCase(2)]
    public async Task SmallArchetypePoolsRepeatOnlyWhenNecessary(int count)
    {
        foreach (var map in GeneratedTournamentMaps.Archetypes.Skip(count)) await coordinator.RemoveMapAsync(map.Uid);
        var tournament = await Create(3);
        await coordinator.StartTournamentAsync(tournament.Id);
        var sequence = tournament.MapsByRound[1];
        Assert.That(sequence.Count, Is.EqualTo(5));
        Assert.That(sequence.Select(m => m.Generation!.Id).Distinct().Count(), Is.EqualTo(5));
        if (count == 2)
            for (var i = 1; i < sequence.Count; i++)
                Assert.That(sequence[i].Generation!.Preset, Is.Not.EqualTo(sequence[i - 1].Generation!.Preset));
    }

    [Test]
    public async Task LegacyStateAndPlayerChoiceKeepOriginalBehavior()
    {
        var old = JsonSerializer.Deserialize<TournamentRecord>("{\"Id\":\"T001\"}")!;
        Assert.That(old.MapSelection, Is.EqualTo(TournamentMapSelection.PlayerChoice));
        Assert.That(old.MapsByRound, Is.Empty);
        var tournament = await Create(2, selection: TournamentMapSelection.PlayerChoice);
        await coordinator.StartTournamentAsync(tournament.Id);
        Assert.That(tournament.MapsByRound, Is.Empty);
        Assert.That(await coordinator.GetRecentMatchesAsync(), Is.Empty);
        foreach (var id in tournament.SeriesIds)
            Assert.That((await coordinator.GetSeriesAsync(id))!.Status, Is.EqualTo(TournamentSeriesStatus.AwaitingMapPicks));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.CreateTournamentAsync("Invalid", TournamentFormat.SingleElimination,
            mapSelection: (TournamentMapSelection)99));
    }
}
