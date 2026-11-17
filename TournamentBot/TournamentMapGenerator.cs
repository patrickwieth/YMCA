using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ymca.TournamentBot;

public sealed record GeneratedMapArtifact(string Path, string Uid, string Sha256);

public sealed class TournamentMapGenerator
{
    readonly OpenRaServerConfiguration config;
    // Serializes host generation, including simultaneous series sharing a round decider.
    readonly SemaphoreSlim gate = new(1, 1);
    readonly Func<GeneratedMapSpec, string, CancellationToken, Task> generate;

    public TournamentMapGenerator(OpenRaServerConfiguration config,
        Func<GeneratedMapSpec, string, CancellationToken, Task>? generate = null)
    {
        this.config = config;
        this.generate = generate ?? GenerateWithUtilityAsync;
    }

    public async Task<GeneratedMapArtifact> GetAsync(GeneratedMapSpec spec, CancellationToken token)
    {
        GeneratedTournamentMaps.Validate(spec);
        await gate.WaitAsync(token);
        try
        {
            var root = Path.Combine(config.MatchDirectory, "generated-maps");
            var final = Path.Combine(root, spec.Id);
            if (Directory.Exists(final)) return await ValidateAsync(final, spec, token);
            Directory.CreateDirectory(root);
            var temporary = Path.Combine(root, ".generating-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            try
            {
                await generate(spec, temporary, token);
                await File.WriteAllTextAsync(Path.Combine(temporary, "request.json"), JsonSerializer.Serialize(spec), token);
                await ValidateAsync(temporary, spec, token);
                Directory.Move(temporary, final);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            }
            return await ValidateAsync(final, spec, token);
        }
        finally { gate.Release(); }
    }

    static async Task<GeneratedMapArtifact> ValidateAsync(string directory, GeneratedMapSpec spec, CancellationToken token)
    {
        var request = JsonSerializer.Deserialize<GeneratedMapSpec>(await File.ReadAllTextAsync(Path.Combine(directory, "request.json"), token))
            ?? throw new InvalidDataException("Missing generation request.");
        if (request.Id != spec.Id || request.Preset != spec.Preset || request.Seed != spec.Seed ||
            request.Players != spec.Players || request.Protocol != spec.Protocol)
            throw new InvalidDataException("Saved map request differs from tournament state; refusing to regenerate.");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "map.json"), token));
        var result = document.RootElement;
        var uid = result.GetProperty("Uid").GetString() ?? "";
        var expectedHash = result.GetProperty("Sha256").GetString() ?? "";
        if (result.GetProperty("Protocol").GetInt32() != spec.Protocol ||
            result.GetProperty("Preset").GetString() != spec.Preset || result.GetProperty("Seed").GetInt32() != spec.Seed ||
            result.GetProperty("Players").GetInt32() != spec.Players || !Regex.IsMatch(uid, "\\A[0-9a-f]{40}\\z") ||
            !Regex.IsMatch(expectedHash, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Invalid tournament map generation receipt.");
        var path = Path.Combine(directory, "generated.oramap");
        if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("Generated map exceeds 32 MB.");
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(await sha.ComputeHashAsync(stream, token)).ToLowerInvariant();
        if (hash != expectedHash) throw new InvalidDataException("Saved tournament map checksum mismatch; refusing to regenerate.");
        return new GeneratedMapArtifact(path, uid, hash);
    }

    async Task GenerateWithUtilityAsync(GeneratedMapSpec spec, string output, CancellationToken token)
    {
        var info = new ProcessStartInfo(config.UseDotNetHost ? "dotnet" : config.UtilityExecutable)
        {
            WorkingDirectory = config.WorkingDirectory, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        if (config.UseDotNetHost) info.ArgumentList.Add(config.UtilityExecutable);
        foreach (var arg in new[] { config.ModId, "--generate-tournament-map", spec.Preset,
            spec.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture), spec.Players.ToString(), output })
            info.ArgumentList.Add(arg);
        info.Environment["ENGINE_DIR"] = config.EngineDirectory;
        info.Environment["MOD_SEARCH_PATHS"] = string.Join(',', config.ModSearchPaths.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => Path.GetFullPath(Path.IsPathRooted(path.Trim()) ? path.Trim() : Path.Combine(config.WorkingDirectory, path.Trim()))));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(config.MapGenerationTimeoutSeconds));
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start tournament map generator.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var outputText = await stdout;
            var errors = await stderr;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Tournament map generator exited {process.ExitCode}: {errors}\n{outputText}");
        }
        finally
        {
            if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(CancellationToken.None); }
            await Task.WhenAll(stdout, stderr);
        }
    }
}
