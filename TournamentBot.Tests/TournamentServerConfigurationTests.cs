using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentServerConfigurationTests
{
    [Test]
    public void HostPackagingVersionMatchesClientRelease()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "mod.config"))) root = root.Parent;
        Assert.That(root, Is.Not.Null, "Repository root must be available for packaging checks.");
        using var notes = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "mods/ca/release-notes.json")));
        var version = notes.RootElement.GetProperty("tag_name").GetString();
        Assert.That(File.ReadAllText(Path.Combine(root.FullName, "TournamentBot/Dockerfile")), Does.Contain("ARG YMCA_VERSION=" + version));
        Assert.That(File.ReadAllText(Path.Combine(root.FullName, "TournamentBot/deploy/docker-compose.yml")), Does.Contain("YMCA_VERSION: " + version));
    }

    [Test]
    public async Task AssignedMapIsTheOnlyAllowedLobbyMap()
    {
        var config = new OpenRaServerConfiguration
        {
            WorkingDirectory = Path.GetTempPath(), EngineDirectory = Path.GetTempPath(),
            ServerExecutable = Path.Combine(Path.GetTempPath(), "engine/bin/OpenRA.Server.dll"),
            ModSearchPaths = Path.GetTempPath(), UseDotNetHost = true
        };
        await using var pool = new OpenRaServerPool(config, new ReplayMetadataReader(config));
        var match = new MatchRecord
        {
            Id = "CONFIG", MapUid = new string('a', 40), Port = 12400,
            PlayerOneOpenRaName = "One", PlayerTwoOpenRaName = "Two"
        };
        var create = typeof(OpenRaServerPool).GetMethod("CreateServerStartInfo", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var info = (ProcessStartInfo)create.Invoke(pool, new object[] { match })!;
        Assert.That(info.ArgumentList, Does.Contain("Server.Map=" + match.MapUid));
        Assert.That(info.ArgumentList, Does.Contain("Server.MapPool=" + match.MapUid));
        Assert.That(info.ArgumentList, Does.Contain("Server.ExitOnGameOver=True"));
        Assert.That(info.ArgumentList, Does.Contain("Server.GameOverShutdownDelaySeconds=180"));
    }
}
