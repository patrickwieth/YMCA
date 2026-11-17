using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentServerConfigurationTests
{
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
    }
}
