using System.Net.Http.Headers;
using System.Text.Json;

namespace Ymca.TournamentBot;

public sealed record PublishedRelease(string Version, string ReleaseUrl, string Name, string Notes);

public sealed class ReleaseFeedClient : IDisposable
{
    readonly ReleaseAnnouncementConfiguration config;
    readonly HttpClient client;

    public ReleaseFeedClient(ReleaseAnnouncementConfiguration config, HttpMessageHandler? handler = null)
    {
        this.config = config;
        client = handler == null ? new HttpClient() : new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("YMCA-Overlord", "1.0"));
    }

    public async Task<PublishedRelease> GetLatestAsync(CancellationToken cancellationToken)
    {
        var manifest = await client.GetStringAsync(config.ManifestUrl, cancellationToken);
        var (version, releaseUrl) = ParseManifest(manifest);
        var name = $"YMCA {version}";
        var notes = "";

        try
        {
            var tag = Uri.EscapeDataString(version);
            var apiUrl = $"https://api.github.com/repos/{config.GitHubRepository}/releases/tags/{tag}";
            using var response = await client.GetAsync(apiUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = document.RootElement;
                if (root.TryGetProperty("name", out var releaseName) && !string.IsNullOrWhiteSpace(releaseName.GetString()))
                    name = releaseName.GetString()!;
                if (root.TryGetProperty("html_url", out var htmlUrl) && !string.IsNullOrWhiteSpace(htmlUrl.GetString()))
                    releaseUrl = htmlUrl.GetString()!;
                if (root.TryGetProperty("body", out var body))
                    notes = body.GetString() ?? "";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            Console.WriteLine($"[{DateTime.Now:O}] Could not load GitHub release notes for {version}: {ex.Message}");
        }

        return new PublishedRelease(version, releaseUrl, name, notes);
    }

    public static (string Version, string ReleaseUrl) ParseManifest(string manifest)
    {
        string? version = null;
        string? releaseUrl = null;
        foreach (var rawLine in manifest.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                version = line["Version:".Length..].Trim();
            else if (line.StartsWith("ReleaseUrl:", StringComparison.OrdinalIgnoreCase))
                releaseUrl = line["ReleaseUrl:".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(releaseUrl))
            throw new InvalidDataException("The stable release manifest does not contain Version and ReleaseUrl values.");
        if (!Uri.TryCreate(releaseUrl, UriKind.Absolute, out _))
            throw new InvalidDataException("The stable release manifest contains an invalid ReleaseUrl.");

        return (version, releaseUrl);
    }

    public void Dispose() => client.Dispose();
}
