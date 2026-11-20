using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ymca.TournamentBot;

public sealed class RemoteHostClient : IDisposable
{
    readonly HttpClient http;
    readonly RemoteHostConfiguration config;
    bool verified;

    public RemoteHostClient(RemoteHostConfiguration config, HttpMessageHandler? handler = null)
    {
        config.Validate();
        this.config = config;
        http = handler == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) : new HttpClient(handler);
        http.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.GetToken());
    }

    public async Task EnsureCreatedAsync(MatchRecord match, CancellationToken token)
    {
        if (!verified)
        {
            using var health = await http.GetAsync("v1/health", token);
            Check(health);
            using var data = JsonDocument.Parse(await health.Content.ReadAsStringAsync(token));
            if (data.RootElement.GetProperty("Protocol").GetInt32() != 1 ||
                data.RootElement.GetProperty("GameVersion").GetString() != config.ExpectedGameVersion ||
                data.RootElement.GetProperty("PublicHost").GetString() != config.PublicHost)
                throw new InvalidDataException("Remote host protocol, game version, or public hostname does not match configuration.");
            verified = true;
        }
        using (var existing = await http.GetAsync("v1/matches/" + match.Id, token))
        {
            if (existing.StatusCode != HttpStatusCode.NotFound)
            {
                Check(existing);
                var snapshot = await existing.Content.ReadFromJsonAsync<RemoteMatchSnapshot>(JsonOptions.Default, token)
                    ?? throw new InvalidDataException("Empty remote status.");
                if (snapshot.Match.RemoteRequestId != match.RemoteRequestId ||
                    RemoteHostProtocol.RequestHash(RemoteHostProtocol.SanitizeRequest(snapshot.Match)) !=
                    RemoteHostProtocol.RequestHash(RemoteHostProtocol.SanitizeRequest(match)))
                    throw new InvalidDataException("Remote match identity differs; refusing duplicate creation.");
                return;
            }
        }
        if (match.Generation != null && match.GeneratedMapSha256.Length != 0)
        {
            if (string.IsNullOrEmpty(match.GeneratedMapPath) || !File.Exists(match.GeneratedMapPath))
                throw new InvalidDataException("Original generated map is required for remote hosting.");
            await using var stream = File.OpenRead(match.GeneratedMapPath);
            if (stream.Length > 32 * 1024 * 1024) throw new InvalidDataException("Map exceeds upload limit.");
            using var content = new StreamContent(stream);
            using var uploaded = await http.PutAsync("v1/maps/" + match.Generation.Id + "/" + match.GeneratedMapSha256, content, token);
            Check(uploaded);
        }
        using var response = await http.PostAsJsonAsync("v1/matches", RemoteHostProtocol.SanitizeRequest(match), JsonOptions.Default, token);
        Check(response);
    }

    public async Task<RemoteMatchSnapshot> GetAsync(MatchRecord match, CancellationToken token)
    {
        using var response = await http.GetAsync("v1/matches/" + match.Id, token);
        Check(response);
        var snapshot = await response.Content.ReadFromJsonAsync<RemoteMatchSnapshot>(JsonOptions.Default, token)
            ?? throw new InvalidDataException("Empty remote match status.");
        if (snapshot.Match.Id != match.Id || snapshot.Match.RemoteRequestId != match.RemoteRequestId)
            throw new InvalidDataException("Remote host returned a different match identity.");
        return snapshot;
    }

    public async Task ArmAsync(MatchRecord match, CancellationToken token)
    {
        using var response = await http.PutAsJsonAsync("v1/matches/" + match.Id + "/attendance", match.AttendanceTimer, JsonOptions.Default, token);
        // Lobby may have started between status fetch and timer update: never force a forfeit.
        if (response.StatusCode != HttpStatusCode.Conflict) Check(response);
    }

    public async Task DownloadAsync(MatchRecord match, bool replay, string destination, string? expectedSha256, CancellationToken token)
    {
        var limit = replay ? 512L * 1024 * 1024 : 32L * 1024 * 1024;
        using var response = await http.GetAsync("v1/matches/" + match.Id + (replay ? "/replay" : "/map"), HttpCompletionOption.ResponseHeadersRead, token);
        Check(response);
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Remote artifact exceeds size limit.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".download";
        try
        {
            await using (var input = await response.Content.ReadAsStreamAsync(token))
            await using (var output = File.Create(temporary))
            {
                var buffer = new byte[64 * 1024];
                long total = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, token)) != 0)
                {
                    total += count;
                    if (total > limit) throw new InvalidDataException("Remote artifact exceeds size limit.");
                    await output.WriteAsync(buffer.AsMemory(0, count), token);
                }
            }
            if (expectedSha256 != null)
            {
                await using var stream = File.OpenRead(temporary);
                using var sha = SHA256.Create();
                var actual = Convert.ToHexString(await sha.ComputeHashAsync(stream, token)).ToLowerInvariant();
                if (actual != expectedSha256) throw new InvalidDataException("Remote map checksum mismatch.");
            }
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    static void Check(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Conflict ||
            (int)response.StatusCode is >= 300 and < 400)
            throw new InvalidDataException($"Remote hosting request rejected (HTTP {(int)response.StatusCode}); no local fallback was started.");
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => http.Dispose();
}
