using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot;

// A dedicated hosting agent: no Discord token, bracket mutation, or winner decisions.
public sealed class RemoteGameHost : IAsyncDisposable
{
    readonly GameHostConfiguration config;
    readonly SemaphoreSlim gate = new(1, 1);
    readonly Dictionary<string, RemoteMatchSnapshot> matches = new(StringComparer.Ordinal);
    readonly OpenRaServerPool pool;
    WebApplication? app;

    public RemoteGameHost(GameHostConfiguration config)
    {
        this.config = config;
        if (config.Server.RemoteHost != null)
            throw new InvalidDataException("A game host cannot recursively delegate matches.");
        config.Server.NormalizeAndValidate();
        pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server), config.MapDownloadBaseUrl, config.AllowRelayCredentials, allowPreparedMaps: true);
        pool.ServerStarting += m => UpdateAsync(m, s => s.Match.Status = MatchStatus.StartingServer);
        pool.ServerReady += m => UpdateAsync(m, s => s.Match.Status = MatchStatus.WaitingForPlayers);
        pool.MatchStarted += m => UpdateAsync(m, s => s.Match.Status = MatchStatus.Playing);
        pool.ServerStopped += m => UpdateAsync(m, s => s.Stopped = true);
        pool.ServerFailed += (m, error) => UpdateAsync(m, s => { s.Match.Status = MatchStatus.Failed; s.Match.FailureReason = error; s.Stopped = true; });
        pool.ResultAvailable += (m, result) => UpdateAsync(m, s => { s.Result = result; s.Match.Status = MatchStatus.AwaitingConfirmation; });
        pool.AttendanceDecisionAvailable += async (m, decision) =>
        {
            await UpdateAsync(m, s => s.AttendanceDecision = decision);
            return true;
        };
    }

    public async Task StartAsync(CancellationToken token)
    {
        var secret = Environment.GetEnvironmentVariable(config.TokenEnvironmentVariable);
        if (secret == null || secret.Length < 32)
            throw new InvalidDataException("Game host token must contain at least 32 characters.");
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes("Bearer " + secret));
        if (File.Exists(config.StateFile))
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, RemoteMatchSnapshot>>(await File.ReadAllTextAsync(config.StateFile, token), JsonOptions.Default)
                ?? throw new InvalidDataException("Empty remote host state.");
            foreach (var pair in saved)
            {
                matches.Add(pair.Key, pair.Value);
                // Never invent a replacement game after an agent restart. The prior
                // process is gone; preserve results, otherwise require admin recovery.
                if (!pair.Value.Stopped && pair.Value.Match.Status != MatchStatus.Queued)
                {
                    pair.Value.Stopped = true;
                    if (pair.Value.Result == null && pair.Value.AttendanceDecision == null)
                    {
                        pair.Value.Match.Status = MatchStatus.Failed;
                        pair.Value.Match.FailureReason = "Hosting agent restarted; game was not restarted automatically.";
                    }
                }
            }
        }
        await SaveAsync();
        pool.Start();
        foreach (var snapshot in matches.Values.Where(s => s.Match.Status == MatchStatus.Queued && !s.Stopped))
            await pool.EnqueueAsync(snapshot.Match, token);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders(); // Never log authorization headers, credentials, or request bodies.
        builder.WebHost.UseUrls(config.ListenUrl);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32 * 1024 * 1024);
        app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString()));
            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actual))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next();
        });
        var manifest = Path.Combine(config.Server.WorkingDirectory, "mods", config.Server.ModId, "mod.yaml");
        var version = File.ReadLines(manifest).Select(line => line.Trim())
            .First(line => line.StartsWith("Version:", StringComparison.Ordinal)).Substring("Version:".Length).Trim();
        app.MapGet("/v1/health", () => Results.Json(new { Protocol = 1, GameVersion = version,
            PublicHost = config.Server.PublicHost, Capacity = config.Server.MaxConcurrentServers }, JsonOptions.Default));
        app.MapPut("/v1/maps/{id}/{hash}", UploadMapAsync);
        app.MapPost("/v1/matches", (Func<HttpContext, Task<IResult>>)CreateAsync);
        app.MapGet("/v1/matches/{id}", SnapshotAsync);
        app.MapGet("/v1/matches/{id}/map", (string id) => ArtifactAsync(id, false));
        app.MapGet("/v1/matches/{id}/replay", (string id) => ArtifactAsync(id, true));
        app.MapPut("/v1/matches/{id}/attendance", ArmAttendanceAsync);
        await app.StartAsync(token);
    }

    async Task<IResult> CreateAsync(HttpContext context)
    {
        if (context.Request.ContentLength > 65536) return Results.BadRequest("Invalid request length.");
        var limit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (limit != null && !limit.IsReadOnly) limit.MaxRequestBodySize = 65536;
        MatchRecord request;
        try
        {
            var input = await JsonSerializer.DeserializeAsync<MatchRecord>(context.Request.Body, JsonOptions.Default, context.RequestAborted)
                ?? throw new InvalidDataException("Missing request.");
            request = RemoteHostProtocol.SanitizeRequest(input);
            if (!string.IsNullOrEmpty(input.Password))
            {
                if (!config.AllowRelayCredentials) throw new InvalidDataException("Relay credentials disabled.");
                RemoteHostProtocol.ApplyRelayCredentials(request, input);
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException)
        {
            return Results.BadRequest("Invalid game-host request.");
        }
        await gate.WaitAsync(context.RequestAborted);
        try
        {
            var hash = RemoteHostProtocol.RequestHash(request);
            if (matches.TryGetValue(request.Id, out var existing))
                return existing.RequestHash == hash ? Results.Ok() : Results.Conflict("Match identity is already in use.");
            if (matches.Values.Count(s => !s.Stopped) >= config.Server.MaxConcurrentServers * 4)
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            if (request.Generation != null && request.GeneratedMapSha256.Length != 0)
            {
                var artifact = Path.Combine(config.Server.MatchDirectory, "uploads", request.Generation.Id, request.GeneratedMapSha256 + ".oramap");
                if (!File.Exists(artifact)) return Results.Conflict("Upload the original immutable map before starting this job.");
                request.GeneratedMapPath = artifact;
            }
            request.HostingPublicHost = config.Server.PublicHost;
            var snapshot = new RemoteMatchSnapshot { RequestHash = hash, Match = request };
            matches.Add(request.Id, snapshot);
            try { await SaveAsync(); }
            catch { matches.Remove(request.Id); throw; }
            // Commit before enqueue: a lost response/retry cannot create a duplicate.
            await pool.EnqueueAsync(request, CancellationToken.None);
            return Results.Accepted();
        }
        finally { gate.Release(); }
    }

    async Task<IResult> UploadMapAsync(string id, string hash, HttpContext context)
    {
        if (!Guid.TryParseExact(id, "N", out _) || !System.Text.RegularExpressions.Regex.IsMatch(hash, "\\A[0-9a-f]{64}\\z"))
            return Results.BadRequest();
        var root = Path.Combine(config.Server.MatchDirectory, "uploads", id);
        Directory.CreateDirectory(root);
        var temporary = Path.Combine(root, Guid.NewGuid().ToString("N") + ".upload");
        try
        {
            await using (var output = File.Create(temporary))
                await context.Request.Body.CopyToAsync(output, context.RequestAborted);
            if (new FileInfo(temporary).Length is <= 0 or > 32 * 1024 * 1024) return Results.BadRequest();
            using var sha = SHA256.Create();
            await using (var input = File.OpenRead(temporary))
                if (Convert.ToHexString(await sha.ComputeHashAsync(input, context.RequestAborted)).ToLowerInvariant() != hash)
                    return Results.BadRequest("Map checksum mismatch.");
            File.Move(temporary, Path.Combine(root, hash + ".oramap"), true);
            return Results.Ok();
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    async Task<IResult> SnapshotAsync(string id)
    {
        await gate.WaitAsync();
        try
        {
            if (!matches.TryGetValue(id, out var snapshot)) return Results.NotFound();
            snapshot.AttendanceDecision ??= TournamentAttendanceFiles.Read(snapshot.Match);
            var lobby = Path.Combine(snapshot.Match.SupportDirectory, "lobby-status.json");
            snapshot.LobbyJson = null;
            if (!string.IsNullOrEmpty(snapshot.Match.SupportDirectory) && File.Exists(lobby) && new FileInfo(lobby).Length < 64 * 1024)
            {
                try { snapshot.LobbyJson = await File.ReadAllTextAsync(lobby); }
                catch (IOException) { }
            }
            // Serialize under the gate rather than exposing mutable state to ASP.NET.
            return Results.Text(JsonSerializer.Serialize(snapshot, JsonOptions.Default), "application/json");
        }
        finally { gate.Release(); }
    }

    async Task<IResult> ArtifactAsync(string id, bool replay)
    {
        await gate.WaitAsync();
        try
        {
            if (!matches.TryGetValue(id, out var s)) return Results.NotFound();
            var path = replay ? s.Result?.ReplayPath : s.Match.GeneratedMapPath;
            var root = Path.GetFullPath(config.Server.MatchDirectory) + Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(path) || !Path.GetFullPath(path).StartsWith(root, StringComparison.Ordinal) || !File.Exists(path))
                return Results.NotFound();
            return Results.File(path, "application/octet-stream");
        }
        finally { gate.Release(); }
    }

    async Task<IResult> ArmAttendanceAsync(string id, HttpContext context)
    {
        var timer = await JsonSerializer.DeserializeAsync<MatchAttendanceTimer>(context.Request.Body, JsonOptions.Default, context.RequestAborted);
        if (timer == null || timer.Id.Length > 100 || timer.DeadlineUtc.Kind != DateTimeKind.Utc)
            return Results.BadRequest();
        await gate.WaitAsync(context.RequestAborted);
        try
        {
            if (!matches.TryGetValue(id, out var snapshot)) return Results.NotFound();
            if (snapshot.Stopped || snapshot.Match.Status != MatchStatus.WaitingForPlayers) return Results.Conflict();
            snapshot.Match.AttendanceTimer = timer;
            TournamentAttendanceFiles.Arm(snapshot.Match);
            await SaveAsync();
            return Results.Ok();
        }
        finally { gate.Release(); }
    }

    async Task UpdateAsync(MatchRecord match, Action<RemoteMatchSnapshot> change)
    {
        await gate.WaitAsync();
        try { change(matches[match.Id]); await SaveAsync(); }
        finally { gate.Release(); }
    }

    async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(config.StateFile))!);
        var temporary = config.StateFile + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(matches, JsonOptions.Default));
        File.Move(temporary, config.StateFile, true);
    }

    public async ValueTask DisposeAsync()
    {
        if (app != null) await app.DisposeAsync();
        await pool.DisposeAsync();
        gate.Dispose();
    }
}
