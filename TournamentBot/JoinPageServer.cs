using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Ymca.TournamentBot;

public sealed class JoinPageServer : IAsyncDisposable
{
    readonly BotConfiguration config;
    readonly TournamentCoordinator coordinator;
    WebApplication? application;

    public JoinPageServer(BotConfiguration config, TournamentCoordinator coordinator)
    {
        this.config = config;
        this.coordinator = coordinator;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!config.JoinPage.Enabled)
            return;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(config.JoinPage.ListenUrl);
        application = builder.Build();
        application.MapGet("/join/{matchId}", RenderJoinPageAsync);
        application.MapGet("/spectate/{matchId}", RenderSpectatorPageAsync);
        application.MapGet("/replay/{matchId}", RenderReplayPageAsync);
        application.MapGet("/replay/{matchId}/download", DownloadReplayAsync);
        await application.StartAsync(cancellationToken);
    }

    async Task RenderJoinPageAsync(HttpContext context, string matchId)
    {
        var match = await coordinator.GetMatchAsync(matchId);
        if (match == null || match.Port == null || string.IsNullOrEmpty(match.Password)
            || match.Status is MatchStatus.Completed or MatchStatus.Cancelled or MatchStatus.Failed)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Match not found or no longer active.");
            return;
        }

        if (!ulong.TryParse(context.Request.Query["player"], out var playerId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("This join link does not identify a tournament participant.");
            return;
        }

        var playerName = playerId switch
        {
            _ when playerId == match.PlayerOneDiscordId => match.PlayerOneOpenRaName,
            _ when playerId == match.PlayerTwoDiscordId => match.PlayerTwoOpenRaName,
            _ when playerId == match.PlayerOneTeammateDiscordId => match.PlayerOneTeammateOpenRaName,
            _ when playerId == match.PlayerTwoTeammateDiscordId => match.PlayerTwoTeammateOpenRaName,
            _ => null
        };
        if (playerName == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("This player is not registered for the match.");
            return;
        }

        var tournament = match.TournamentId == null ? null : await coordinator.GetTournamentAsync(match.TournamentId);
        var host = config.Server.PublicHost;
        var joinUri = $"ymca://{host}:{match.Port}?password={Uri.EscapeDataString(match.Password)}&name={Uri.EscapeDataString(playerName)}";
        var lobby = ReadLobbyStatus(match);
        var playerRows = RenderPlayerRow(match.PlayerOneOpenRaName, match.PlayerOneTeamName, lobby)
            + RenderPlayerRow(match.PlayerOneTeammateOpenRaName, match.PlayerOneTeamName, lobby)
            + RenderPlayerRow(match.PlayerTwoOpenRaName, match.PlayerTwoTeamName, lobby)
            + RenderPlayerRow(match.PlayerTwoTeammateOpenRaName, match.PlayerTwoTeamName, lobby);
        var liveState = lobby?.State == "GameStarted" ? "Playing" : lobby == null ? match.Status.ToString() : "Lobby";
        var countdown = lobby?.AutoStartAtUtc is DateTime startAt
            ? $"<p class=\"countdown\">Game starts in approximately {Math.Max(0, (int)Math.Ceiling((startAt - DateTime.UtcNow).TotalSeconds))} seconds</p>"
            : "";
        var html = $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <meta http-equiv=""refresh"" content=""4"">
  <title>Join YMCA match {WebUtility.HtmlEncode(match.Id)}</title>
  <style>
    body {{ font: 18px system-ui; max-width: 680px; margin: 4rem auto; padding: 0 1rem; background: #17191f; color: #eee; }}
    .card {{ background: #242832; padding: 2rem; border-radius: 12px; }}
    .join {{ display: inline-block; padding: .8rem 1.2rem; border-radius: 7px; background: #5865f2; color: white; text-decoration: none; font-weight: 700; }}
    code {{ user-select: all; background: #111319; padding: .15rem .35rem; border-radius: 4px; }}
    table {{ width: 100%; border-collapse: collapse; margin: 1rem 0; }}
    th, td {{ text-align: left; padding: .55rem; border-bottom: 1px solid #3a3f4c; }}
    .ready {{ color: #6ee7a2; }} .waiting {{ color: #f5c96a; }} .countdown {{ color: #6ee7ff; font-weight: 700; }}
    .muted {{ color: #aeb4c0; }}

  </style>
</head>
<body><div class=""card"">
  <h1>YMCA tournament match {WebUtility.HtmlEncode(match.Id)}</h1>
  {(tournament == null ? "" : $"<p>Tournament: <strong>{WebUtility.HtmlEncode(tournament.Name)}</strong> ({WebUtility.HtmlEncode(tournament.Id)})</p>")}
  {(match.TournamentRound > 0 ? $"<p>Round: <strong>{match.TournamentRound}</strong>{(match.IsThirdPlaceMatch ? " — Third-place playoff" : "")}</p>" : "")}
  <p>Map: <strong>{WebUtility.HtmlEncode(match.MapTitle)}</strong></p>
  <p>Match status: <strong>{WebUtility.HtmlEncode(liveState)}</strong></p>
  {countdown}
  <table><thead><tr><th>Player</th><th>Tournament team</th><th>Connection</th><th>Ready</th><th>Team</th><th>Spawn</th></tr></thead>
  <tbody>{playerRows}</tbody></table>
  <p class=""muted"">This page refreshes every four seconds.</p>
  <p>Your player name: <code>{WebUtility.HtmlEncode(playerName)}</code></p>
  <p><a class=""join"" href=""{WebUtility.HtmlEncode(joinUri)}"">Start YMCA and join</a></p>
  <h2>Manual connection</h2>
  <p>Server: <code>{WebUtility.HtmlEncode(host)}:{match.Port}</code></p>
  <p>Password: <code>{WebUtility.HtmlEncode(match.Password)}</code></p>
  <p>Open YMCA → Multiplayer → Direct Connect if the button does not work.</p>
</div></body>
</html>";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    async Task RenderSpectatorPageAsync(HttpContext context, string matchId)
    {
        var match = await coordinator.GetMatchAsync(matchId);
        if (match == null || !match.AllowSpectators || match.Port == null || string.IsNullOrEmpty(match.SpectatorPassword)
            || match.Status is MatchStatus.Completed or MatchStatus.Cancelled or MatchStatus.Failed)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Spectator access is not available for this match.");
            return;
        }

        var spectatorUri = $"ymca://{config.Server.PublicHost}:{match.Port}" +
            $"?password={Uri.EscapeDataString(match.SpectatorPassword)}&spectator=true";
        var lobby = ReadLobbyStatus(match);
        var liveState = lobby?.State == "GameStarted" ? "Playing — spectator joining is closed" : "Waiting for players";
        var html = $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <title>Spectate YMCA match {WebUtility.HtmlEncode(match.Id)}</title>
  <style>
    body {{ font: 18px system-ui; max-width: 680px; margin: 4rem auto; padding: 0 1rem; background: #17191f; color: #eee; }}
    .card {{ background: #242832; padding: 2rem; border-radius: 12px; }}
    .watch {{ display: inline-block; padding: .8rem 1.2rem; border-radius: 7px; background: #5865f2; color: white; text-decoration: none; font-weight: 700; }}
    .muted {{ color: #aeb4c0; }}
  </style>
</head>
<body><div class=""card"">
  <h1>Spectate YMCA match {WebUtility.HtmlEncode(match.Id)}</h1>
  <p>{WebUtility.HtmlEncode(match.PlayerOneOpenRaName)} vs {WebUtility.HtmlEncode(match.PlayerTwoOpenRaName)}</p>
  <p>Map: <strong>{WebUtility.HtmlEncode(match.MapTitle)}</strong></p>
  <p>Status: <strong>{WebUtility.HtmlEncode(liveState)}</strong></p>
  <p><a class=""watch"" href=""{WebUtility.HtmlEncode(spectatorUri)}"">Join as spectator</a></p>
  <p class=""muted"">This link can only create a spectator connection and cannot occupy a player slot.</p>
</div></body>
</html>";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    async Task RenderReplayPageAsync(HttpContext context, string matchId)
    {
        var match = await GetReplayMatchAsync(matchId);
        if (match == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Replay not found.");
            return;
        }

        var downloadUrl = GetPublicReplayDownloadUrl(match.Id);
        var replayUri = $"ymca://replay?url={Uri.EscapeDataString(downloadUrl)}";
        var html = $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <title>Watch YMCA replay {WebUtility.HtmlEncode(match.Id)}</title>
  <style>
    body {{ font: 18px system-ui; max-width: 680px; margin: 4rem auto; padding: 0 1rem; background: #17191f; color: #eee; }}
    .card {{ background: #242832; padding: 2rem; border-radius: 12px; }}
    .watch {{ display: inline-block; padding: .8rem 1.2rem; border-radius: 7px; background: #5865f2; color: white; text-decoration: none; font-weight: 700; }}
    .muted {{ color: #aeb4c0; }}
  </style>
</head>
<body><div class=""card"">
  <h1>YMCA replay {WebUtility.HtmlEncode(match.Id)}</h1>
  <p>{WebUtility.HtmlEncode(match.PlayerOneOpenRaName)} vs {WebUtility.HtmlEncode(match.PlayerTwoOpenRaName)}</p>
  <p>Map: <strong>{WebUtility.HtmlEncode(match.MapTitle)}</strong></p>
  <p><a id=""watch"" class=""watch"" href=""{WebUtility.HtmlEncode(replayUri)}"">Open replay in YMCA</a></p>
  <p class=""muted"">YMCA will download the replay and play it. If nothing happens, click the button above.</p>
  <p class=""muted""><a href=""{WebUtility.HtmlEncode(downloadUrl)}"">Download replay only</a></p>
</div>
<script>window.location.href = document.getElementById('watch').href;</script>
</body>
</html>";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    async Task DownloadReplayAsync(HttpContext context, string matchId)
    {
        var match = await GetReplayMatchAsync(matchId);
        if (match == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Replay not found.");
            return;
        }

        context.Response.ContentType = "application/octet-stream";
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"YMCA-{match.Id}.orarep\"";
        await context.Response.SendFileAsync(match.ReplayPath!);
    }

    async Task<MatchRecord?> GetReplayMatchAsync(string matchId)
    {
        var match = await coordinator.GetMatchAsync(matchId);
        return match?.Status == MatchStatus.Completed
            && !string.IsNullOrEmpty(match.ReplayPath)
            && File.Exists(match.ReplayPath)
                ? match
                : null;
    }

    static LobbyStatus? ReadLobbyStatus(MatchRecord match)
    {
        if (string.IsNullOrEmpty(match.SupportDirectory))
            return null;

        var path = Path.Combine(match.SupportDirectory, "lobby-status.json");
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<LobbyStatus>(File.ReadAllText(path))
                : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string RenderPlayerRow(string expectedName, string teamName, LobbyStatus? lobby)
    {
        if (string.IsNullOrEmpty(expectedName))
            return "";
        var player = lobby?.Players.FirstOrDefault(value =>
            value.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
        var connection = player == null ? "Not connected" : "Connected";
        var ready = player == null ? "—" : player.Ready ? "Ready" : "Not ready";
        var readyClass = player?.Ready == true ? "ready" : "waiting";
        var team = player?.Team > 0 ? player.Team.ToString() : "—";
        var spawn = player?.SpawnPoint > 0 ? player.SpawnPoint.ToString() : "—";
        return $"<tr><td>{WebUtility.HtmlEncode(expectedName)}</td><td>{WebUtility.HtmlEncode(teamName)}</td><td>{connection}</td>" +
            $"<td class=\"{readyClass}\">{ready}</td><td>{team}</td><td>{spawn}</td></tr>";
    }

    public string GetPublicJoinUrl(string matchId, ulong playerId) =>
        $"{config.JoinPage.PublicBaseUrl}/join/{Uri.EscapeDataString(matchId)}?player={playerId}";

    public string GetPublicSpectatorUrl(string matchId) =>
        $"{config.JoinPage.PublicBaseUrl}/spectate/{Uri.EscapeDataString(matchId)}";

    public string GetPublicReplayUrl(string matchId) =>
        $"{config.JoinPage.PublicBaseUrl}/replay/{Uri.EscapeDataString(matchId)}";

    string GetPublicReplayDownloadUrl(string matchId) =>
        $"{config.JoinPage.PublicBaseUrl}/replay/{Uri.EscapeDataString(matchId)}/download";

    sealed class LobbyStatus
    {
        public string State { get; set; } = "";
        public DateTime? AutoStartAtUtc { get; set; }
        public List<LobbyPlayerStatus> Players { get; set; } = new();
    }

    sealed class LobbyPlayerStatus
    {
        public string Name { get; set; } = "";
        public bool Ready { get; set; }
        public bool IsObserver { get; set; }
        public int Team { get; set; }
        public int SpawnPoint { get; set; }
    }

    public async ValueTask DisposeAsync()
    {
        if (application != null)
            await application.DisposeAsync();
    }
}
