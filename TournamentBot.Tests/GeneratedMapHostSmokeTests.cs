using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Ymca.TournamentBot;
using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class GeneratedMapHostSmokeTests
{
    [TestCase(2), TestCase(4)]
    [Explicit("Set YMCA_TOURNAMENT_ENGINE_SMOKE to a built game worktree; starts only its own local, unadvertised server.")]
    public async Task HostGeneratesMapAndStartsDedicatedServerWithExactUid(int players)
    {
        var root = Environment.GetEnvironmentVariable("YMCA_TOURNAMENT_ENGINE_SMOKE")
            ?? throw new InvalidOperationException("Missing YMCA_TOURNAMENT_ENGINE_SMOKE");
        var directory = Path.Combine(Path.GetTempPath(), "ymca-generated-host-smoke", Guid.NewGuid().ToString("N"));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var config = new OpenRaServerConfiguration
        {
            WorkingDirectory = root, MatchDirectory = directory, EngineDirectory = Path.Combine(root, "engine"),
            ServerExecutable = Path.Combine(root, "engine/bin/OpenRA.Server.dll"),
            UtilityExecutable = Path.Combine(root, "engine/bin/OpenRA.Utility.dll"), UseDotNetHost = true,
            ModSearchPaths = Path.Combine(root, "mods") + "," + Path.Combine(root, "engine/mods"),
            MaxConcurrentServers = 1, FirstPort = port, StartupTimeoutSeconds = 90, GenerateTournamentMaps = true
        };
        config.NormalizeAndValidate();
        var selected = GeneratedTournamentMaps.Freeze(GeneratedTournamentMaps.Archetypes.Single(m => m.Uid == "archetype:TwoSides"), players == 2 ? TournamentMode.OneVsOne : TournamentMode.TwoVsTwo);
        selected.Generation!.Seed = 97001;
        var match = new MatchRecord
        {
            Id = "M97001", Generation = selected.Generation, MapUid = selected.Uid, MapTitle = selected.Title,
            PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2, PlayerOneOpenRaName = "ProbeOne", PlayerTwoOpenRaName = "ProbeTwo",
            PlayerOneTeammateDiscordId = players == 4 ? 3UL : null, PlayerTwoTeammateDiscordId = players == 4 ? 4UL : null,
            PlayerOneTeammateOpenRaName = players == 4 ? "ProbeThree" : "", PlayerTwoTeammateOpenRaName = players == 4 ? "ProbeFour" : ""
        };
        var ready = new TaskCompletionSource<MatchRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var pool = new OpenRaServerPool(config, new ReplayMetadataReader(config), TournamentMapDownloadInfo.TrustedOrigin);
        pool.ServerReady += m => { ready.TrySetResult(m); return Task.CompletedTask; };
        pool.ServerFailed += (_, error) => { ready.TrySetException(new Exception(error)); return Task.CompletedTask; };
        pool.Start();
        await pool.EnqueueAsync(match);
        var resolved = await ready.Task.WaitAsync(TimeSpan.FromSeconds(200));
        Assert.That(resolved.MapUid, Does.Match("^[0-9a-f]{40}$"));
        Assert.That(File.Exists(resolved.GeneratedMapPath), Is.True);
        Assert.That(TournamentMapDownloadInfo.TryParse(File.ReadAllText(Path.Combine(resolved.SupportDirectory,
            TournamentMapDownloadInfo.ReceiptFilename)), out var downloadInfo), Is.True);
        Assert.That(downloadInfo.Uid, Is.EqualTo(resolved.MapUid));
        Assert.That(downloadInfo.Sha256, Is.EqualTo(resolved.GeneratedMapSha256));
        Assert.That(File.Exists(Path.Combine(resolved.SupportDirectory, "maps/ca/generated", resolved.MapUid + ".oramap")), Is.True);
        using var log = new StreamReader(new FileStream(Path.Combine(resolved.SupportDirectory, "Logs/dedicated-server.log"),
            FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        Assert.That(await log.ReadToEndAsync(), Does.Contain(resolved.MapUid));
        listener.Start();
        var webPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var botConfig = new BotConfiguration
        {
            Server = config, StateFile = Path.Combine(directory, "state.json"),
            JoinPage = new JoinPageConfiguration { ListenUrl = $"http://127.0.0.1:{webPort}", PublicBaseUrl = $"http://127.0.0.1:{webPort}" }
        };
        var store = new StateStore(botConfig.StateFile);
        resolved.Status = MatchStatus.WaitingForPlayers;
        await store.UpdateAsync(state => state.Matches[resolved.Id] = resolved);
        var coordinator = new TournamentCoordinator(botConfig, store, pool);
        await using var pages = new JoinPageServer(botConfig, coordinator);
        await pages.StartAsync(CancellationToken.None);
        using var http = new HttpClient { BaseAddress = new Uri(botConfig.JoinPage.PublicBaseUrl) };
        var html = await http.GetStringAsync("/join/M97001?player=1");
        Assert.That(html, Does.Contain("/map/M97001/download"));
        Assert.That(html, Does.Contain("automatic download in the lobby"));
        Assert.That(html, Does.Not.Contain("install before joining"));
        var downloaded = await http.GetByteArrayAsync("/map/M97001/download");
        Assert.That(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(downloaded)).ToLowerInvariant(), Is.EqualTo(resolved.GeneratedMapSha256));
        Assert.That((await http.GetAsync("/map/UNKNOWN/download")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        TestContext.Progress.WriteLine($"Generated host and HTTP map download verified: {resolved.MapUid}; evidence retained at {directory}");
    }
}
