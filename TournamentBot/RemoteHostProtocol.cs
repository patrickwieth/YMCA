using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot;

public sealed class RemoteHostConfiguration
{
    public string Id { get; set; } = "worlds";
    public string BaseUrl { get; set; } = "";
    public string PublicHost { get; set; } = "";
    public string ExpectedGameVersion { get; set; } = "";
    public string TokenEnvironmentVariable { get; set; } = "YMCA_GAME_HOST_TOKEN";
    public int MaxConcurrentServers { get; set; } = 8;

    public void Validate()
    {
        if (!Regex.IsMatch(Id, "\\A[a-zA-Z0-9_-]{1,40}\\z") || Id == "local")
            throw new InvalidDataException("Remote host ID is invalid.");
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidDataException("Remote host API requires HTTPS (HTTP only on loopback).");
        if (Uri.CheckHostName(PublicHost) == UriHostNameType.Unknown || MaxConcurrentServers is < 1 or > 64)
            throw new InvalidDataException("Remote public host or capacity is invalid.");
        if (string.IsNullOrWhiteSpace(ExpectedGameVersion))
            throw new InvalidDataException("Remote host expected game version is required.");
        if (string.IsNullOrWhiteSpace(TokenEnvironmentVariable))
            throw new InvalidDataException("Remote host token environment variable is required.");
    }

    public string GetToken()
    {
        var token = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
        if (token == null || token.Length < 32)
            throw new InvalidDataException("Remote host API token must contain at least 32 characters.");
        return token;
    }
}

public sealed class GameHostConfiguration
{
    // Temporary bridge for a running legacy controller. Credentials are still
    // accepted only over the authenticated API, never via public join requests.
    public bool AllowRelayCredentials { get; set; }
    public string ListenUrl { get; set; } = "http://127.0.0.1:5081";
    public string TokenEnvironmentVariable { get; set; } = "YMCA_GAME_HOST_TOKEN";
    public string StateFile { get; set; } = "/data/host-state.json";
    public string MapDownloadBaseUrl { get; set; } = "";
    public OpenRaServerConfiguration Server { get; set; } = new();
}

public sealed class RemoteMatchSnapshot
{
    public string RequestHash { get; set; } = "";
    public MatchRecord Match { get; set; } = new();
    public bool Stopped { get; set; }
    public ReplayResult? Result { get; set; }
    public TournamentAttendanceDecision? AttendanceDecision { get; set; }
    public string? LobbyJson { get; set; }
}

public static class RemoteHostProtocol
{
    public static bool ValidMatchId(string id) => Regex.IsMatch(id, "\\AM[0-9]{4,12}\\z");

    // Explicit allowlist: remote requests must never control filesystem paths, ports,
    // passwords, executable names, server arguments, or existing match results.
    public static MatchRecord SanitizeRequest(MatchRecord input)
    {
        if (!ValidMatchId(input.Id) || !Guid.TryParseExact(input.RemoteRequestId, "N", out _))
            throw new InvalidDataException("Invalid match/request identity.");
        if (input.PlayerOneDiscordId == 0 || input.PlayerTwoDiscordId == 0 || input.PlayerOneDiscordId == input.PlayerTwoDiscordId)
            throw new InvalidDataException("Invalid participants.");
        var names = new[] { input.PlayerOneOpenRaName, input.PlayerTwoOpenRaName,
            input.PlayerOneTeammateOpenRaName, input.PlayerTwoTeammateOpenRaName };
        if (names.Take(2).Any(string.IsNullOrWhiteSpace) || names.Any(n => n.Length > 80 || n.Any(char.IsControl) || n.Contains(',')))
            throw new InvalidDataException("Invalid player names.");
        if (input.Generation != null)
            GeneratedTournamentMaps.Validate(input.Generation);
        if (!Regex.IsMatch(input.MapUid, "\\A[0-9a-f]{40}\\z") &&
            (input.Generation == null || input.MapUid != "generated:" + input.Generation.Id))
            throw new InvalidDataException("Invalid map UID.");
        if (input.GeneratedMapSha256.Length != 0 && !Regex.IsMatch(input.GeneratedMapSha256, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Invalid map checksum.");
        return new MatchRecord
        {
            Id = input.Id, RemoteRequestId = input.RemoteRequestId,
            PlayerOneDiscordId = input.PlayerOneDiscordId, PlayerTwoDiscordId = input.PlayerTwoDiscordId,
            PlayerOneOpenRaName = input.PlayerOneOpenRaName, PlayerTwoOpenRaName = input.PlayerTwoOpenRaName,
            PlayerOneTeammateDiscordId = input.PlayerOneTeammateDiscordId, PlayerTwoTeammateDiscordId = input.PlayerTwoTeammateDiscordId,
            PlayerOneTeammateOpenRaName = input.PlayerOneTeammateOpenRaName, PlayerTwoTeammateOpenRaName = input.PlayerTwoTeammateOpenRaName,
            Generation = input.Generation, MapUid = input.MapUid, MapTitle = input.MapTitle,
            GeneratedMapSha256 = input.GeneratedMapSha256, AllowSpectators = input.AllowSpectators,
            Status = MatchStatus.Queued, CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void ApplyRelayCredentials(MatchRecord request, MatchRecord input)
    {
        if (!Regex.IsMatch(input.Password, "\\A[a-zA-Z0-9_-]{16,128}\\z") ||
            input.AllowSpectators && !Regex.IsMatch(input.SpectatorPassword, "\\A[a-zA-Z0-9_-]{16,128}\\z"))
            throw new InvalidDataException("Invalid relay credentials.");
        request.Password = input.Password;
        request.SpectatorPassword = input.AllowSpectators ? input.SpectatorPassword : "";
    }

    public static string RequestHash(MatchRecord sanitized)
    {
        // Map UID is resolved after generation. Identity uses the immutable generation
        // recipe instead, so reconnecting to an already resolved job remains idempotent.
        var identity = new { sanitized.Id, sanitized.RemoteRequestId,
            sanitized.PlayerOneDiscordId, sanitized.PlayerTwoDiscordId,
            sanitized.PlayerOneOpenRaName, sanitized.PlayerTwoOpenRaName,
            sanitized.PlayerOneTeammateDiscordId, sanitized.PlayerTwoTeammateDiscordId,
            sanitized.PlayerOneTeammateOpenRaName, sanitized.PlayerTwoTeammateOpenRaName,
            sanitized.Generation, Map = sanitized.Generation == null ? sanitized.MapUid : "generated",
            sanitized.AllowSpectators, sanitized.Password, sanitized.SpectatorPassword };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identity)));
    }

    public static string PublicHost(MatchRecord match, OpenRaServerConfiguration fallback) =>
        string.IsNullOrEmpty(match.HostingPublicHost) ? fallback.PublicHost : match.HostingPublicHost;
}
