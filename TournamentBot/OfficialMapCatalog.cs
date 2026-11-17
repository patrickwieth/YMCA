using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ymca.TournamentBot;

public sealed record OfficialMap(string Uid, int PlayerCount, string Title);

public sealed class OfficialMapCatalog
{
    static readonly Regex MapLine = new("^(?<uid>[0-9a-f]{40})\\t(?<players>[0-9]+)\\t(?<title>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    readonly OpenRaServerConfiguration config;
    IReadOnlyList<OfficialMap> maps = Array.Empty<OfficialMap>();

    public OfficialMapCatalog(OpenRaServerConfiguration config)
    {
        this.config = config;
    }

    public IReadOnlyList<OfficialMap> Maps => maps;

    public OfficialMap? Get(string uid) => maps.FirstOrDefault(map =>
        map.Uid.Equals(uid, StringComparison.OrdinalIgnoreCase));

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo
        {
            FileName = config.UseDotNetHost ? "dotnet" : config.UtilityExecutable,
            WorkingDirectory = Path.GetDirectoryName(config.UtilityExecutable)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (config.UseDotNetHost)
            info.ArgumentList.Add(config.UtilityExecutable);
        info.ArgumentList.Add(config.ModId);
        info.ArgumentList.Add("--list-maps");
        info.Environment["ENGINE_DIR"] = config.EngineDirectory;
        info.Environment["MOD_SEARCH_PATHS"] = string.Join(',', config.ModSearchPaths
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => Path.GetFullPath(Path.IsPathRooted(path.Trim())
                ? path.Trim()
                : Path.Combine(config.WorkingDirectory, path.Trim()))));

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("OpenRA.Utility could not be started to load the map catalog.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"OpenRA.Utility map catalog failed: {error.Trim()}");

        maps = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => MapLine.Match(line.TrimEnd('\r')))
            .Where(match => match.Success)
            .Select(match => new OfficialMap(
                match.Groups["uid"].Value,
                int.Parse(match.Groups["players"].Value),
                match.Groups["title"].Value))
            .GroupBy(map => map.Uid, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(map => map.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (maps.Count == 0)
            throw new InvalidOperationException("OpenRA.Utility returned no official YMCA maps.");

        Console.WriteLine($"Loaded {maps.Count} official YMCA maps.");
    }
}
