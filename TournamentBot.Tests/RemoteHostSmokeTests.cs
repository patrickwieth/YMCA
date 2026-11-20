using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class RemoteHostSmokeTests
{
    [Test]
    public async Task NativeRemoteHostGeneratesMapAndSurvivesControllerDisconnect()
    {
        var url = Environment.GetEnvironmentVariable("YMCA_REMOTE_SMOKE_URL");
        if (string.IsNullOrEmpty(url)) Assert.Ignore("Requires an isolated remote agent; never run against a live tournament agent.");
        var remote = new RemoteHostConfiguration
        {
            Id = "smoke", BaseUrl = url!, PublicHost = Environment.GetEnvironmentVariable("YMCA_REMOTE_SMOKE_HOST")!,
            ExpectedGameVersion = "v0.97.6", TokenEnvironmentVariable = "YMCA_REMOTE_SMOKE_TOKEN"
        };
        var directory = Path.Combine(Path.GetTempPath(), "ymca-remote-smoke-" + Guid.NewGuid().ToString("N"));
        var config = new OpenRaServerConfiguration { RemoteHost = remote, MatchDirectory = directory, ReplayPollSeconds = 1, MaxConcurrentServers = 1 };
        var generation = new GeneratedMapSpec { Id = Guid.NewGuid().ToString("N"), Preset = "Continents", Seed = 20261120, Players = 2 };
        var match = new MatchRecord
        {
            Id = "M900000001", PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2,
            PlayerOneOpenRaName = "PreflightA", PlayerTwoOpenRaName = "PreflightB",
            Generation = generation, MapUid = "generated:" + generation.Id, MapTitle = "Remote smoke Continents"
        };
        try
        {
            var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var pool = new OpenRaServerPool(config, new ReplayMetadataReader(config)))
            {
                pool.ServerReady += m => { ready.TrySetResult(true); return Task.CompletedTask; };
                pool.ServerFailed += (m,e) => { ready.TrySetException(new InvalidOperationException(e)); return Task.CompletedTask; };
                pool.Start();
                await pool.EnqueueAsync(match);
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(150));
                Assert.That(match.HostingPublicHost, Is.EqualTo(remote.PublicHost));
                Assert.That(match.Password, Is.Not.Empty);
                Assert.That(match.Port, Is.Not.Null);
                Assert.That(File.Exists(match.GeneratedMapPath), Is.True);
                Assert.That(match.GeneratedMapSha256, Has.Length.EqualTo(64));
                Assert.That(match.MapUid, Does.Not.StartWith("generated:"));
            }
            using var client = new RemoteHostClient(remote);
            var afterDisconnect = await client.GetAsync(match, CancellationToken.None);
            Assert.That(afterDisconnect.Stopped, Is.False, "Stopping the controller must not kill a remote game.");
            Assert.That(afterDisconnect.Match.Password, Is.EqualTo(match.Password));
            await client.EnsureCreatedAsync(match, CancellationToken.None);
            var reattached = await client.GetAsync(match, CancellationToken.None);
            Assert.That(reattached.Match.Port, Is.EqualTo(match.Port));
            Assert.That(reattached.Match.Password, Is.EqualTo(match.Password));
            TestContext.Out.WriteLine($"Remote smoke passed: {match.Id}, {match.MapUid}, port {match.Port}; credentials retained across reconnect.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
