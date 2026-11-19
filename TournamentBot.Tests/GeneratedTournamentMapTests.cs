using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class GeneratedTournamentMapTests
{
    string directory = null!;
    BotConfiguration config = null!;
    StateStore store = null!;
    OpenRaServerPool pool = null!;
    TournamentCoordinator coordinator = null!;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "ymca-generated-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        config = new BotConfiguration
        {
            StateFile = Path.Combine(directory, "state.json"),
            Server = new OpenRaServerConfiguration { GenerateTournamentMaps = true, MatchDirectory = Path.Combine(directory, "matches") }
        };
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

    async Task<TournamentRecord> Start(int wins, int players = 2)
    {
        var tournament = await coordinator.CreateTournamentAsync("Generated", TournamentFormat.SingleElimination, winsRequired: wins);
        for (ulong i = 1; i <= (ulong)players; i++)
        {
            await coordinator.RegisterAsync(i, "Player " + i, "P" + i);
            await coordinator.JoinTournamentAsync(tournament.Id, i);
        }
        return await coordinator.StartTournamentAsync(tournament.Id);
    }

    [Test]
    public async Task ArchetypePoolCommandsAndAutocompleteIgnoreLegacyMapsAndPersist()
    {
        await store.UpdateAsync(state => state.MapPool.Add(new TournamentMap { Uid = "old", Title = "Dune Patrol", PlayerCount = 2 }));
        var active = await coordinator.GetMapPoolAsync();
        Assert.That(active.Count, Is.EqualTo(9));
        var official = new[] { new OfficialMap("old", 2, "Dune Patrol") };
        Assert.That(MapPoolAutocomplete.Search(true, false, active, official, "").All(c => c.Value.StartsWith("archetype:")), Is.True);
        Assert.That(MapPoolAutocomplete.Search(true, false, active, official, "mountain").Single().Value, Is.EqualTo("archetype:MountainValleys"));
        Assert.That(MapPoolAutocomplete.Search(true, true, active, official, ""), Is.Empty);
        await coordinator.RemoveMapAsync("ARCHETYPE:MountainValleys");
        active = await coordinator.GetMapPoolAsync();
        Assert.That(active.Count, Is.EqualTo(8));
        Assert.That(MapPoolAutocomplete.Search(true, false, active, official, "mountain"), Is.Empty);
        Assert.That(MapPoolAutocomplete.Search(true, true, active, official, "").Single().Value, Is.EqualTo("archetype:MountainValleys"));
        var restored = new StateStore(config.StateFile);
        await restored.LoadAsync();
        var restarted = new TournamentCoordinator(config, restored, pool);
        Assert.That((await restarted.GetMapPoolAsync()).Count, Is.EqualTo(8));
        await restarted.AddMapAsync("archetype:MountainValleys", "Untrusted title");
        Assert.That((await restarted.GetMapPoolAsync()).Single(m => m.Uid == "archetype:MountainValleys").Title, Is.EqualTo("Mountain Valleys"));
        Assert.That(await restored.ReadAsync(s => s.MapPool.Single().Title), Is.EqualTo("Dune Patrol"));
    }

    [Test]
    public async Task PoolChangesApplyAtStartButLeaveRunningTournamentSnapshotUnchanged()
    {
        var running = await Start(2);
        var pending = await coordinator.CreateTournamentAsync("Next", TournamentFormat.SingleElimination, winsRequired: 2);
        await coordinator.JoinTournamentAsync(pending.Id, 1);
        await coordinator.JoinTournamentAsync(pending.Id, 2);
        await coordinator.RemoveMapAsync("archetype:TwoSides");
        var next = await coordinator.StartTournamentAsync(pending.Id);
        Assert.That(next.MapPool.Any(m => m.Uid == "archetype:TwoSides"), Is.False);
        Assert.That((await coordinator.GetTournamentAsync(running.Id))!.MapPool.Any(m => m.Uid == "archetype:TwoSides"), Is.True);
        var series = (await coordinator.GetSeriesAsync(next.SeriesIds.Single()))!;
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, "archetype:TwoSides"));
    }

    [Test]
    public async Task EmptyPoolBlocksCreationAndStartWithoutResettingToDefaults()
    {
        var pending = await coordinator.CreateTournamentAsync("Next", TournamentFormat.SingleElimination);
        for (ulong i = 1; i <= 2; i++)
        {
            await coordinator.RegisterAsync(i, "P" + i, "P" + i);
            await coordinator.JoinTournamentAsync(pending.Id, i);
        }
        foreach (var map in GeneratedTournamentMaps.Archetypes) await coordinator.RemoveMapAsync(map.Uid);
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.CreateTournamentAsync("Empty", TournamentFormat.SingleElimination));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartTournamentAsync(pending.Id));
        var restored = new StateStore(config.StateFile); await restored.LoadAsync();
        Assert.That(await new TournamentCoordinator(config, restored, pool).GetMapPoolAsync(), Is.Empty);
        Assert.That((await coordinator.GetTournamentAsync(pending.Id))!.Status, Is.EqualTo(TournamentStatus.Registration));
        await coordinator.AddMapAsync("archetype:Arabia", "ignored");
        Assert.That((await coordinator.StartTournamentAsync(pending.Id)).MapPool.Single().Title, Is.EqualTo("Plains"));
    }

    [Test]
    public async Task InvalidAndDuplicateCommandsDoNotModifyEitherPool()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.AddMapAsync("archetype:TwoSides", "Two Sides"));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.AddMapAsync("old-map-uid", "Old map"));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RemoveMapAsync("old-map-uid"));
        await coordinator.RemoveMapAsync("archetype:TwoSides");
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RemoveMapAsync("archetype:TwoSides"));
        Assert.That((await coordinator.GetMapPoolAsync()).Count, Is.EqualTo(8));
    }

    [Test]
    public async Task OldStateWithoutArchetypeSettingsEnablesDefaults()
    {
        await File.WriteAllTextAsync(config.StateFile, "{\"MapPool\":[{\"Uid\":\"old\",\"Title\":\"Dune Patrol\"}]}");
        await store.LoadAsync();
        Assert.That((await coordinator.GetMapPoolAsync()).Count, Is.EqualTo(9));
        Assert.That(await store.ReadAsync(s => s.MapPool.Single().Uid), Is.EqualTo("old"));
    }

    [Test]
    public async Task ConcurrentRemovalsOnlyDisableAnArchetypeOnce()
    {
        async Task<bool> Remove()
        {
            try { await coordinator.RemoveMapAsync("archetype:TwoSides"); return true; }
            catch (InvalidOperationException) { return false; }
        }
        Assert.That((await Task.WhenAll(Remove(), Remove())).Count(success => success), Is.EqualTo(1));
        Assert.That(await store.ReadAsync(s => s.DisabledMapArchetypes.Count), Is.EqualTo(1));
        Assert.That((await coordinator.GetMapPoolAsync()).Count, Is.EqualTo(8));
    }

    [Test]
    public async Task LegacyPoolAndSuggestionsRemainIndependent()
    {
        await coordinator.RemoveMapAsync("archetype:TwoSides");
        config.Server.GenerateTournamentMaps = false;
        await coordinator.AddMapAsync("old", "Dune Patrol", 2);
        var active = await coordinator.GetMapPoolAsync();
        Assert.That(active.Single().Uid, Is.EqualTo("old"));
        var official = new[] { new OfficialMap("old", 2, "Dune Patrol"), new OfficialMap("other", 4, "All That Glitters") };
        Assert.That(MapPoolAutocomplete.Search(false, false, active, official, "dune").Single().Value, Is.EqualTo("old"));
        Assert.That(MapPoolAutocomplete.Search(false, true, active, official, "").Single().Value, Is.EqualTo("other"));
        await coordinator.RemoveMapAsync("old");
        Assert.That(await coordinator.GetMapPoolAsync(), Is.Empty);
        config.Server.GenerateTournamentMaps = true;
        Assert.That((await coordinator.GetMapPoolAsync()).Count, Is.EqualTo(8));
    }

    [TestCase(2)]
    [TestCase(3)]
    public async Task PlayersPickTypesAndAllSeriesShareOnePersistedDecider(int wins)
    {
        var tournament = await Start(wins, 4);
        Assert.That(tournament.UseGeneratedMaps, Is.True);
        Assert.That(tournament.MapPool.Count, Is.EqualTo(9));
        var decider = tournament.RandomMapByRound[1].Generation!;
        var allPickedIds = new List<string>();
        foreach (var id in tournament.SeriesIds)
        {
            var series = (await coordinator.GetSeriesAsync(id))!;
            for (var pick = 0; pick < wins - 1; pick++)
            {
                await coordinator.SubmitSeriesMapPickAsync(id, series.PlayerOneDiscordId, "archetype:TwoSides");
                await coordinator.SubmitSeriesMapPickAsync(id, series.PlayerTwoDiscordId, "archetype:TwoSides");
            }
            series = (await coordinator.GetSeriesAsync(id))!;
            Assert.That(series.Maps.Count, Is.EqualTo(wins * 2 - 1));
            Assert.That(series.Maps.Last().Generation!.Id, Is.EqualTo(decider.Id));
            Assert.That(series.Maps.Last().Generation!.Seed, Is.EqualTo(decider.Seed));
            Assert.That(series.Maps.Take(wins * 2 - 2).All(m => m.Generation!.Preset == "TwoSides"), Is.True);
            allPickedIds.AddRange(series.Maps.Take(wins * 2 - 2).Select(m => m.Generation!.Id));
        }
        Assert.That(allPickedIds.Distinct().Count(), Is.EqualTo(allPickedIds.Count));
        var reloaded = new StateStore(config.StateFile);
        await reloaded.LoadAsync();
        var restored = await reloaded.ReadAsync(s => s.Tournaments[tournament.Id].RandomMapByRound[1].Generation!);
        Assert.That(restored.Id, Is.EqualTo(decider.Id));
        Assert.That(restored.Seed, Is.EqualTo(decider.Seed));
    }

    [Test]
    public async Task Bo1UsesTheRoundMapAndRematchesRetainTheExactRequest()
    {
        var tournament = await Start(1, 4);
        var matches = await coordinator.GetRecentMatchesAsync();
        Assert.That(matches.Select(m => m.Generation!.Id).Distinct(), Is.EqualTo(new[] { tournament.RandomMapByRound[1].Generation!.Id }));
        var first = matches[0];
        await coordinator.SubmitReportAsync(first.Id, first.PlayerOneDiscordId, PlayerReport.Rematch);
        await coordinator.SubmitReportAsync(first.Id, first.PlayerTwoDiscordId, PlayerReport.Rematch);
        var rematch = (await coordinator.GetRecentMatchesAsync()).Single(m => m.ParentMatchId == first.Id);
        Assert.That(rematch.Generation!.Id, Is.EqualTo(first.Generation!.Id));
        Assert.That(rematch.Generation.Seed, Is.EqualTo(first.Generation.Seed));
    }

    [Test]
    public async Task OneToOneScoreQueuesFrozenDeciderWithoutAnotherMapChoice()
    {
        var tournament = await Start(2);
        var series = (await coordinator.GetSeriesAsync(tournament.SeriesIds.Single()))!;
        var decider = tournament.RandomMapByRound[1].Generation!;
        await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, "archetype:TwoSides");
        await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerTwoDiscordId, "archetype:Arabia");
        var first = (await coordinator.GetRecentMatchesAsync()).Single();
        await coordinator.ResolveAsync(first.Id, series.PlayerOneDiscordId);
        var second = (await coordinator.GetRecentMatchesAsync()).Single(m => m.SeriesGameNumber == 2);
        await coordinator.ResolveAsync(second.Id, series.PlayerTwoDiscordId);
        var third = (await coordinator.GetRecentMatchesAsync()).Single(m => m.SeriesGameNumber == 3);
        Assert.That(third.SeriesId, Is.EqualTo(series.Id));
        Assert.That(third.Generation!.Id, Is.EqualTo(decider.Id));
        Assert.That(third.Generation.Seed, Is.EqualTo(decider.Seed));
        Assert.That(tournament.SeriesIds.Count, Is.EqualTo(1));
        Assert.That(series.Status, Is.EqualTo(TournamentSeriesStatus.Playing));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, "__random__"));
    }

    [Test]
    public async Task SeriesEndingEarlyDoesNotQueueTheDecider()
    {
        var tournament = await Start(2);
        var series = (await coordinator.GetSeriesAsync(tournament.SeriesIds.Single()))!;
        await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, "__random__");
        await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerTwoDiscordId, "archetype:Arabia");
        var first = (await coordinator.GetRecentMatchesAsync()).Single();
        await coordinator.ResolveAsync(first.Id, series.PlayerOneDiscordId);
        var second = (await coordinator.GetRecentMatchesAsync()).Single(m => m.Id != first.Id);
        await coordinator.ResolveAsync(second.Id, series.PlayerOneDiscordId);
        Assert.That((await coordinator.GetRecentMatchesAsync()).Count, Is.EqualTo(2));
        Assert.That((await coordinator.GetTournamentAsync(tournament.Id))!.Status, Is.EqualTo(TournamentStatus.Completed));
    }

    [Test]
    public async Task ConcurrentDeciderRequestsAndRestartReuseOneVerifiedPackage()
    {
        var spec = GeneratedTournamentMaps.Freeze(GeneratedTournamentMaps.Archetypes[0], TournamentMode.TwoVsTwo).Generation!;
        Assert.That(spec.Players, Is.EqualTo(4));
        var calls = 0;
        async Task Generate(GeneratedMapSpec request, string output, CancellationToken token)
        {
            calls++;
            await Task.Delay(25, token);
            await WritePackage(request, output, token);
        }
        var generator = new TournamentMapGenerator(config.Server, Generate);
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => generator.GetAsync(spec, CancellationToken.None)));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(results.Select(r => r.Path).Distinct().Count(), Is.EqualTo(1));
        var restarted = new TournamentMapGenerator(config.Server, (_, _, _) => throw new Exception("Must not regenerate"));
        Assert.That(await restarted.GetAsync(spec, CancellationToken.None), Is.EqualTo(results[0]));
        await File.AppendAllTextAsync(results[0].Path, "corruption");
        Assert.ThrowsAsync<InvalidDataException>(() => restarted.GetAsync(spec, CancellationToken.None));
    }

    [Test]
    public async Task FailedGenerationDoesNotPublishPartialMapOrChangeSeed()
    {
        var spec = GeneratedTournamentMaps.Freeze(GeneratedTournamentMaps.Archetypes[0], TournamentMode.OneVsOne).Generation!;
        var seed = spec.Seed;
        var failed = new TournamentMapGenerator(config.Server, async (_, output, token) =>
        {
            await File.WriteAllTextAsync(Path.Combine(output, "generated.oramap"), "partial", token);
            throw new InvalidOperationException("Generator failed");
        });
        Assert.ThrowsAsync<InvalidOperationException>(() => failed.GetAsync(spec, CancellationToken.None));
        Assert.That(Directory.GetDirectories(Path.Combine(config.Server.MatchDirectory, "generated-maps")), Is.Empty);
        var retry = new TournamentMapGenerator(config.Server, WritePackage);
        await retry.GetAsync(spec, CancellationToken.None);
        Assert.That(spec.Seed, Is.EqualTo(seed));
    }

    [Test]
    public void RejectsUnknownArchetypesAndUnsafeCacheIds()
    {
        Assert.Throws<InvalidOperationException>(() => GeneratedTournamentMaps.Freeze(new TournamentMap { Uid = "archetype:../x" }, TournamentMode.OneVsOne));
        Assert.Throws<InvalidDataException>(() => GeneratedTournamentMaps.Validate(new GeneratedMapSpec { Id = "../x", Preset = "TwoSides", Seed = 1, Players = 2 }));
    }

    static async Task WritePackage(GeneratedMapSpec request, string output, CancellationToken token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("map " + request.Seed);
        await File.WriteAllBytesAsync(Path.Combine(output, "generated.oramap"), bytes, token);
        await File.WriteAllTextAsync(Path.Combine(output, "map.json"), JsonSerializer.Serialize(new
        {
            request.Protocol, request.Preset, request.Seed, request.Players, Uid = new string('a', 40),
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
        }), token);
    }
}
