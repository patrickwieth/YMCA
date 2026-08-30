using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ymca.TournamentBot;

public sealed class ReplayMetadataReader
{
    static readonly Regex PlayerHeader = new(@"^\s*\d+:\s*$", RegexOptions.Compiled);
    readonly OpenRaServerConfiguration config;

    public ReplayMetadataReader(OpenRaServerConfiguration config)
    {
        this.config = config;
    }

    public async Task<ReplayResult?> TryReadAsync(string replayPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = CreateStartInfo();
            startInfo.ArgumentList.Add(config.ModId);
            startInfo.ArgumentList.Add("--replay-metadata");
            startInfo.ArgumentList.Add(Path.GetFullPath(replayPath));

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);
            var output = await stdout;
            _ = await stderr;

            if (process.ExitCode != 0)
                return null;

            return Parse(replayPath, output);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException)
        {
            // A replay can be observed before OpenRA has finished writing its metadata trailer.
            return null;
        }
    }

    ProcessStartInfo CreateStartInfo()
    {
        var executable = config.UseDotNetHost ? "dotnet" : config.UtilityExecutable;
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = GetEngineWorkingDirectory(config.UtilityExecutable),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (config.UseDotNetHost)
            info.ArgumentList.Add(config.UtilityExecutable);

        info.Environment["MOD_SEARCH_PATHS"] = ResolveModSearchPaths();
        info.Environment["ENGINE_DIR"] = config.EngineDirectory;
        return info;
    }

    public static ReplayResult Parse(string replayPath, string metadata)
    {
        var mapTitle = "";
        var version = "";
        var players = new List<ReplayPlayerResult>();
        var inPlayers = false;
        string? name = null;
        var outcome = "Undefined";
        var isHuman = false;

        void AddPlayer()
        {
            if (name != null)
                players.Add(new ReplayPlayerResult(name, outcome, isHuman));

            name = null;
            outcome = "Undefined";
            isHuman = false;
        }

        foreach (var rawLine in metadata.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();
            if (!inPlayers)
            {
                if (trimmed.StartsWith("MapTitle:", StringComparison.Ordinal))
                    mapTitle = trimmed["MapTitle:".Length..].Trim();
                else if (trimmed.StartsWith("Version:", StringComparison.Ordinal))
                    version = trimmed["Version:".Length..].Trim();
                else if (trimmed == "Players:")
                    inPlayers = true;

                continue;
            }

            if (PlayerHeader.IsMatch(line))
            {
                AddPlayer();
                continue;
            }

            if (trimmed.StartsWith("Name:", StringComparison.Ordinal))
                name = trimmed["Name:".Length..].Trim();
            else if (trimmed.StartsWith("Outcome:", StringComparison.Ordinal))
                outcome = trimmed["Outcome:".Length..].Trim();
            else if (trimmed.StartsWith("IsHuman:", StringComparison.Ordinal))
                isHuman = bool.TryParse(trimmed["IsHuman:".Length..].Trim(), out var parsed) && parsed;
        }

        AddPlayer();
        if (players.Count == 0)
            throw new InvalidDataException("Replay metadata did not contain any players.");

        return new ReplayResult(Path.GetFullPath(replayPath), mapTitle, version, players);
    }

    string ResolveModSearchPaths() => string.Join(',', config.ModSearchPaths.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(path => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(config.WorkingDirectory, path.Trim()))));

    internal static string GetEngineWorkingDirectory(string executable) =>
        Directory.GetParent(Path.GetDirectoryName(executable)
            ?? throw new InvalidDataException("Invalid OpenRA executable path."))?.FullName
        ?? throw new InvalidDataException("Could not determine the OpenRA engine directory.");
}
