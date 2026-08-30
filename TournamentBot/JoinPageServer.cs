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
            _ => null
        };
        if (playerName == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("This player is not registered for the match.");
            return;
        }

        var host = config.Server.PublicHost;
        var joinUri = $"ymca://{host}:{match.Port}?password={Uri.EscapeDataString(match.Password)}&name={Uri.EscapeDataString(playerName)}";
        var lobby = ReadLobbyStatus(match);
        var playerRows = RenderPlayerRow(match.PlayerOneOpenRaName, lobby) + RenderPlayerRow(match.PlayerTwoOpenRaName, lobby);
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
  <p>Map: <strong>{WebUtility.HtmlEncode(match.MapTitle)}</strong></p>
  <p>Match status: <strong>{WebUtility.HtmlEncode(liveState)}</strong></p>
  {countdown}
  <table><thead><tr><th>Player</th><th>Connection</th><th>Ready</th><th>Team</th><th>Spawn</th></tr></thead>
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

    static string RenderPlayerRow(string expectedName, LobbyStatus? lobby)
    {
        var player = lobby?.Players.FirstOrDefault(value =>
            value.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
        var connection = player == null ? "Not connected" : "Connected";
        var ready = player == null ? "—" : player.Ready ? "Ready" : "Not ready";
        var readyClass = player?.Ready == true ? "ready" : "waiting";
        var team = player?.Team > 0 ? player.Team.ToString() : "—";
        var spawn = player?.SpawnPoint > 0 ? player.SpawnPoint.ToString() : "—";
        return $"<tr><td>{WebUtility.HtmlEncode(expectedName)}</td><td>{connection}</td>" +
            $"<td class=\"{readyClass}\">{ready}</td><td>{team}</td><td>{spawn}</td></tr>";
    }

    public string GetPublicJoinUrl(string matchId, ulong playerId) =>
        $"{config.JoinPage.PublicBaseUrl}/join/{Uri.EscapeDataString(matchId)}?player={playerId}";

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
