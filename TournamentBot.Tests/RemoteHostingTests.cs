using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class RemoteHostingTests
{
    static RemoteHostConfiguration Remote(string endpoint = "https://host.example/") => new()
    {
        Id = "worlds", PublicHost = "worlds.example", BaseUrl = endpoint, ExpectedGameVersion = "v-test",
        TokenEnvironmentVariable = "YMCA_REMOTE_TEST_" + Guid.NewGuid().ToString("N")
    };

    static MatchRecord Request() => new()
    {
        Id = "M0123", RemoteRequestId = Guid.NewGuid().ToString("N"),
        PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2,
        PlayerOneOpenRaName = "One", PlayerTwoOpenRaName = "Two",
        MapUid = new string('a', 40)
    };

    [Test]
    public void NewMatchesUseRemoteButLegacyActiveAndPinnedLocalStayLocal()
    {
        var config = new OpenRaServerConfiguration { PublicHost = "old.example", RemoteHost = Remote() };
        var fresh = Request(); fresh.RemoteRequestId = "";
        OpenRaServerPool.AssignHost(fresh, config);
        Assert.That(fresh.HostingHostId, Is.EqualTo("worlds"));
        Assert.That(fresh.HostingPublicHost, Is.EqualTo("worlds.example"));
        var identity = fresh.RemoteRequestId;
        OpenRaServerPool.AssignHost(fresh, config);
        Assert.That(fresh.RemoteRequestId, Is.EqualTo(identity));
        var legacy = Request(); legacy.Port = 12400; legacy.Status = MatchStatus.Playing;
        OpenRaServerPool.AssignHost(legacy, config);
        Assert.That(legacy.HostingHostId, Is.EqualTo("local"));
        Assert.That(legacy.HostingPublicHost, Is.EqualTo("old.example"));
        legacy.Port = null;
        OpenRaServerPool.AssignHost(legacy, config);
        Assert.That(legacy.HostingHostId, Is.EqualTo("local"));
        config.RemoteHost = null;
        Assert.Throws<InvalidDataException>(() => OpenRaServerPool.AssignHost(fresh, config));
    }

    [Test]
    public void RequestSanitizationDoesNotTrustPathsPortsOrCredentials()
    {
        var request = Request();
        request.SupportDirectory = "/etc"; request.GeneratedMapPath = "/etc/passwd";
        request.Port = 22; request.Password = "injected"; request.FinalWinnerDiscordId = 1;
        var sanitized = RemoteHostProtocol.SanitizeRequest(request);
        Assert.Multiple(() =>
        {
            Assert.That(sanitized.SupportDirectory, Is.Empty);
            Assert.That(sanitized.GeneratedMapPath, Is.Empty);
            Assert.That(sanitized.Port, Is.Null);
            Assert.That(sanitized.Password, Is.Empty);
            Assert.That(sanitized.FinalWinnerDiscordId, Is.Null);
            Assert.That(sanitized.Status, Is.EqualTo(MatchStatus.Queued));
        });
        request.Id = "../M0123";
        Assert.Throws<InvalidDataException>(() => RemoteHostProtocol.SanitizeRequest(request));
    }

    [Test]
    public void RelayCredentialsAreValidatedAndPartOfRequestIdentity()
    {
        var input = Request(); input.Password = new string('p', 24);
        var request = RemoteHostProtocol.SanitizeRequest(input);
        var before = RemoteHostProtocol.RequestHash(request);
        RemoteHostProtocol.ApplyRelayCredentials(request, input);
        Assert.That(request.Password, Is.EqualTo(input.Password));
        Assert.That(RemoteHostProtocol.RequestHash(request), Is.Not.EqualTo(before));
        input.Password = "unsafe\nvalue";
        Assert.Throws<InvalidDataException>(() => RemoteHostProtocol.ApplyRelayCredentials(request, input));
    }

    [Test]
    public void ControllerCanAdoptLegacyRelayWithoutStartingAnotherGame()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var m = Request(); m.RemoteRequestId = ""; m.Port = 12400;
        var id = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.Combine(root,m.Id));
        try
        {
            File.WriteAllText(Path.Combine(root,m.Id,"remote-relay.json"), JsonSerializer.Serialize(new
                { HostId="worlds",PublicHost="worlds.example",RequestId=id,RemotePort=12405 }));
            OpenRaServerPool.AssignHost(m,new OpenRaServerConfiguration { MatchDirectory=root,RemoteHost=Remote() });
            Assert.That(m.HostingHostId,Is.EqualTo("worlds"));
            Assert.That(m.RemoteRequestId,Is.EqualTo(id));
            Assert.That(m.Port,Is.EqualTo(12405));
        }
        finally { Directory.Delete(root,true); }
    }

    [Test]
    public void GeneratedRequestIdentitySurvivesMapResolutionButNotParticipantChanges()
    {
        var request = Request();
        request.Generation = new GeneratedMapSpec { Id = Guid.NewGuid().ToString("N"), Preset = "Continents", Seed = 10, Players = 2 };
        request.MapUid = "generated:" + request.Generation.Id;
        var before = RemoteHostProtocol.RequestHash(RemoteHostProtocol.SanitizeRequest(request));
        request.MapUid = new string('b', 40);
        request.GeneratedMapSha256 = new string('a', 64);
        Assert.That(RemoteHostProtocol.RequestHash(RemoteHostProtocol.SanitizeRequest(request)), Is.EqualTo(before));
        request.PlayerTwoOpenRaName = "Three";
        Assert.That(RemoteHostProtocol.RequestHash(RemoteHostProtocol.SanitizeRequest(request)), Is.Not.EqualTo(before));
    }

    [TestCase("http://remote.example/")]
    [TestCase("https://user:pass@host.example/")]
    [TestCase("https://host.example/?token=secret")]
    public void UnsafeApiEndpointsAreRejected(string endpoint) =>
        Assert.Throws<InvalidDataException>(() => Remote(endpoint).Validate());

    [Test]
    public async Task AgentRequiresAuthenticationAndDeduplicatesConcurrentRequests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-remote-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "mods", "ca"));
        File.WriteAllText(Path.Combine(directory, "mods", "ca", "mod.yaml"), "Metadata:\n\tVersion: v-test\n");
        var executable = Path.Combine(directory, "not-an-executable");
        File.WriteAllText(executable, "deliberately not executable");
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var remote = Remote("http://127.0.0.1:" + port);
        Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, new string('t', 48));
        var config = new GameHostConfiguration
        {
            ListenUrl = remote.BaseUrl, TokenEnvironmentVariable = remote.TokenEnvironmentVariable,
            StateFile = Path.Combine(directory, "state.json"),
            Server = new OpenRaServerConfiguration
            {
                WorkingDirectory = directory, MatchDirectory = "matches", PublicHost = remote.PublicHost,
                ServerExecutable = executable, UtilityExecutable = executable, MaxConcurrentServers = 1
            }
        };
        try
        {
            await using var agent = new RemoteGameHost(config);
            await agent.StartAsync(CancellationToken.None);
            using var http = new HttpClient { BaseAddress = new Uri(remote.BaseUrl) };
            using (var denied = await http.GetAsync("/v1/health")) Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new string('t', 48));
            using var health = await http.GetAsync("/v1/health");
            var json = JsonDocument.Parse(await health.Content.ReadAsStringAsync());
            Assert.That(json.RootElement.GetProperty("GameVersion").GetString(), Is.EqualTo("v-test"));
            var mapId = Guid.NewGuid().ToString("N");
            var package = Encoding.UTF8.GetBytes("immutable test package");
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(package)).ToLowerInvariant();
            using (var bad = await http.PutAsync("/v1/maps/" + mapId + "/" + new string('0',64), new ByteArrayContent(package)))
                Assert.That(bad.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            using (var uploaded = await http.PutAsync("/v1/maps/" + mapId + "/" + hash, new ByteArrayContent(package)))
                Assert.That(uploaded.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(File.ReadAllBytes(Path.Combine(config.Server.MatchDirectory,"uploads",mapId,hash+".oramap")), Is.EqualTo(package));
            var request = Request();
            var replies = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => http.PostAsJsonAsync("/v1/matches", request)));
            Assert.That(replies.Count(r => r.StatusCode == HttpStatusCode.Accepted), Is.EqualTo(1));
            Assert.That(replies.Count(r => r.StatusCode == HttpStatusCode.OK), Is.EqualTo(4));
            foreach (var reply in replies) reply.Dispose();
            request.PlayerTwoOpenRaName = "Different";
            using var conflict = await http.PostAsJsonAsync("/v1/matches", request);
            Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            request.Id = "../../etc";
            using var invalid = await http.PostAsJsonAsync("/v1/matches", request);
            Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            using var saved = JsonDocument.Parse(File.ReadAllText(config.StateFile));
            Assert.That(saved.RootElement.EnumerateObject().Count(), Is.EqualTo(1));
        }
        finally
        {
            Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, null);
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task ClientRejectsWrongVersionBeforeCreatingAnyMatch()
    {
        var remote = Remote();
        Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, new string('x', 48));
        var posts = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post) posts++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Protocol\":1,\"GameVersion\":\"wrong\",\"PublicHost\":\"worlds.example\"}") };
        });
        try
        {
            using var client = new RemoteHostClient(remote, handler);
            Assert.ThrowsAsync<InvalidDataException>(async () => await client.EnsureCreatedAsync(Request(), CancellationToken.None));
            Assert.That(posts, Is.Zero);
            await Task.CompletedTask;
        }
        finally { Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, null); }
    }

    [Test]
    public async Task BadMapChecksumNeverPublishesAnArtifact()
    {
        var remote = Remote();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, new string('x', 48));
        try
        {
            using var client = new RemoteHostClient(remote, new StubHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("wrong map")) }));
            var path = Path.Combine(directory, "generated.oramap");
            Assert.ThrowsAsync<InvalidDataException>(async () => await client.DownloadAsync(Request(), false, path, new string('0',64), CancellationToken.None));
            Assert.That(File.Exists(path), Is.False);
            Assert.That(File.Exists(path + ".download"), Is.False);
            await Task.CompletedTask;
        }
        finally
        {
            Environment.SetEnvironmentVariable(remote.TokenEnvironmentVariable, null);
            if (Directory.Exists(directory)) Directory.Delete(directory,true);
        }
    }

    sealed class StubHandler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage, HttpResponseMessage> respond;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => this.respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
