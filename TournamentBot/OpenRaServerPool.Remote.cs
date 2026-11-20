using System.Text.Json;

namespace Ymca.TournamentBot;

public sealed partial class OpenRaServerPool
{
    async Task RemoteWorkerAsync(CancellationToken token)
    {
        using var client = new RemoteHostClient(config.RemoteHost!);
        await foreach (var match in remoteQueue.Reader.ReadAllAsync(token))
        {
            try { await RunRemoteMatchAsync(client, match, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception ex) { await InvokeAsync(ServerFailed, match, ex.Message); }
        }
    }

    async Task RunRemoteMatchAsync(RemoteHostClient client, MatchRecord match, CancellationToken token)
    {
        match.SupportDirectory = Path.Combine(config.MatchDirectory, match.Id);
        Directory.CreateDirectory(match.SupportDirectory);
        // Persist host/request identity before any remote side effect. On reconnection
        // POST is idempotent, and disposing this client does NOT stop the remote game.
        await InvokeAsync(ServerStarting, match);
        var created = false;
        var ready = false;
        var started = false;
        var resultDelivered = false;
        var attendanceDelivered = false;
        string? armedTimer = null;
        var lastWarning = DateTime.MinValue;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!created)
                {
                    await client.EnsureCreatedAsync(match, token);
                    created = true;
                }
                var remote = await client.GetAsync(match, token);
                var hosted = remote.Match;
                if (hosted.Status == MatchStatus.Failed)
                    throw new InvalidDataException(hosted.FailureReason ?? "Remote game server failed.");
                if (hosted.HostingPublicHost != config.RemoteHost!.PublicHost)
                    throw new InvalidDataException("Remote host advertised an unexpected public hostname.");

                if (remote.LobbyJson != null)
                {
                    if (remote.LobbyJson.Length > 64 * 1024) throw new InvalidDataException("Oversized remote lobby status.");
                    using var document = JsonDocument.Parse(remote.LobbyJson);
                    var path = Path.Combine(match.SupportDirectory, "lobby-status.json");
                    await File.WriteAllTextAsync(path + ".tmp", remote.LobbyJson, token);
                    File.Move(path + ".tmp", path, true);
                }

                if (!ready && hosted.Status is MatchStatus.WaitingForPlayers or MatchStatus.Playing or MatchStatus.AwaitingConfirmation)
                {
                    if (hosted.Port is not (>= 1 and <= 65535) || string.IsNullOrEmpty(hosted.Password))
                        throw new InvalidDataException("Remote server returned incomplete join credentials.");
                    if (!match.MapUid.StartsWith("generated:", StringComparison.Ordinal) && match.MapUid != hosted.MapUid ||
                        match.GeneratedMapSha256.Length != 0 && match.GeneratedMapSha256 != hosted.GeneratedMapSha256)
                        throw new InvalidDataException("Remote host changed the immutable match map.");
                    if (match.Generation != null)
                    {
                        var path = Path.Combine(config.MatchDirectory, "generated-maps", "remote-" + match.RemoteRequestId, "generated.oramap");
                        if (hosted.GeneratedMapSha256.Length != 64) throw new InvalidDataException("Missing remote map checksum.");
                        await client.DownloadAsync(match, false, path, hosted.GeneratedMapSha256, token);
                        match.GeneratedMapPath = path;
                        match.GeneratedMapSha256 = hosted.GeneratedMapSha256;
                    }
                    match.MapUid = hosted.MapUid;
                    match.Port = hosted.Port;
                    match.Password = hosted.Password;
                    match.SpectatorPassword = hosted.SpectatorPassword;
                    await InvokeAsync(ServerStarting, match); // Persist map before any join invitation.
                    if (!remote.Stopped && remote.Result == null)
                        await InvokeAsync(ServerReady, match);
                    ready = true;
                }
                if (!started && hosted.Status == MatchStatus.Playing)
                {
                    await InvokeAsync(MatchStarted, match);
                    started = true;
                }
                if (match.AttendanceTimer is { EligibleForForfeit: true, Outcome: "Pending" } timer &&
                    timer.Id != armedTimer && hosted.Status == MatchStatus.WaitingForPlayers)
                {
                    await client.ArmAsync(match, token);
                    armedTimer = timer.Id;
                }
                if (remote.AttendanceDecision != null)
                {
                    var path = Path.Combine(match.SupportDirectory, OpenRA.Mods.CA.MapGeneration.TournamentAttendanceProtocol.DecisionFile);
                    await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(remote.AttendanceDecision), token);
                    File.Move(path + ".tmp", path, true);
                    if (!attendanceDelivered && remote.AttendanceDecision.Outcome is "OneWins" or "TwoWins" && AttendanceDecisionAvailable != null)
                    {
                        await AttendanceDecisionAvailable(match, remote.AttendanceDecision);
                        attendanceDelivered = true;
                    }
                }
                if (!resultDelivered && remote.Result != null)
                {
                    var path = Path.Combine(match.SupportDirectory, "remote.orarep");
                    await client.DownloadAsync(match, true, path, null, token);
                    match.ReplayPath = path;
                    await InvokeAsync(ResultAvailable, match, remote.Result with { ReplayPath = path });
                    resultDelivered = true;
                }
                if (remote.Stopped)
                {
                    await InvokeAsync(ServerStopped, match);
                    if (!resultDelivered && !attendanceDelivered)
                        throw new InvalidDataException("Remote game stopped without a result; manual review required.");
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException && !token.IsCancellationRequested)
            {
                // An unavailable host is not permission to rehost the match. Keep the
                // durable request identity and retry; the remote game keeps running.
                if (DateTime.UtcNow - lastWarning > TimeSpan.FromSeconds(30))
                {
                    Console.Error.WriteLine($"Remote host {match.HostingHostId} unavailable for {match.Id}; retrying without starting a replacement.");
                    lastWarning = DateTime.UtcNow;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(config.ReplayPollSeconds), token);
        }
    }
}
