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
