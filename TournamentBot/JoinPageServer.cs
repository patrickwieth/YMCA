using System.Net;
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

        var host = config.Server.PublicHost;
        var joinUri = $"ymca://{host}:{match.Port}?password={Uri.EscapeDataString(match.Password)}";
        var html = $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <title>Join YMCA match {WebUtility.HtmlEncode(match.Id)}</title>
  <style>
    body {{ font: 18px system-ui; max-width: 680px; margin: 4rem auto; padding: 0 1rem; background: #17191f; color: #eee; }}
    .card {{ background: #242832; padding: 2rem; border-radius: 12px; }}
    .join {{ display: inline-block; padding: .8rem 1.2rem; border-radius: 7px; background: #5865f2; color: white; text-decoration: none; font-weight: 700; }}
    code {{ user-select: all; background: #111319; padding: .15rem .35rem; border-radius: 4px; }}
  </style>
</head>
<body><div class=""card"">
  <h1>YMCA tournament match {WebUtility.HtmlEncode(match.Id)}</h1>
  <p>Map: <strong>{WebUtility.HtmlEncode(match.MapTitle)}</strong></p>
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

    public string GetPublicJoinUrl(string matchId) => $"{config.JoinPage.PublicBaseUrl}/join/{Uri.EscapeDataString(matchId)}";

    public async ValueTask DisposeAsync()
    {
        if (application != null)
            await application.DisposeAsync();
    }
}
